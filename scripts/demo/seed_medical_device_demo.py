#!/usr/bin/env python3
"""Loads the fictional AsterCare Medical demo into a running local HR system, through the public API.

It signs in as the HR administrator from .env and does what HR would do in the app:
  1. creates the Pulse One project (with a fictional project lead),
  2. creates company and project knowledge articles and uploads one generated PDF/DOCX to each,
  3. waits for the virus scan, then approves each document for AI as "NormalInternal",
  4. builds and publishes the Company package and the Pulse One project package,
  5. creates a draft quiz for each package and, when AI is configured, queues AI question generation.

Run it again safely: anything that already exists is reused. Credentials are read from .env and never printed.
Usage:  python3 scripts/demo/seed_medical_device_demo.py   [--api http://127.0.0.1:8080]
"""

from __future__ import annotations

import argparse
import http.cookiejar
import json
import os
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path
from typing import Any, Dict, List, Optional

sys.path.insert(0, str(Path(__file__).resolve().parent))
from document_writer import write_docx, write_pdf  # noqa: E402
from medical_device_content import COMPANY, COMPANY_DOCUMENTS, DISCLAIMER, PROJECT, PROJECT_DOCUMENTS  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
COMPANY_PACKAGE = f"{COMPANY} company onboarding"
PROJECT_PACKAGE = f"{PROJECT['name']} project onboarding"
CONTENT_TYPES = {
    "pdf": "application/pdf",
    "docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
}


class ApiError(Exception):
    def __init__(self, status: int, body: str):
        super().__init__(f"HTTP {status}: {body[:400]}")
        self.status = status


class Api:
    def __init__(self, base: str):
        self.base = base.rstrip("/") + "/api/v1"
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        self.token = ""

    def login(self, email: str, password: str) -> None:
        self.call("POST", "/auth/login", {"email": email, "password": password, "rememberMe": False}, csrf=False)
        self.token = self.call("GET", "/auth/antiforgery")["token"]

    def call(self, method: str, path: str, body: Any = None, etag: Optional[int] = None, csrf: bool = True,
             raw: Optional[bytes] = None, content_type: Optional[str] = None) -> Any:
        data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
        request = urllib.request.Request(self.base + path, data=data, method=method)
        if data is not None:
            request.add_header("Content-Type", content_type or "application/json")
        if csrf and method != "GET" and self.token:
            request.add_header("X-CSRF-TOKEN", self.token)
        if etag is not None:
            request.add_header("If-Match", f'"{etag}"')
        try:
            with self.opener.open(request, timeout=60) as response:
                text = response.read().decode()
                return json.loads(text) if text else None
        except urllib.error.HTTPError as error:
            raise ApiError(error.code, error.read().decode(errors="replace")) from None

    def upload(self, path: str, file_path: Path, content_type: str, document_type: str) -> Any:
        boundary = uuid.uuid4().hex
        parts = [
            f'--{boundary}\r\nContent-Disposition: form-data; name="documentType"\r\n\r\n{document_type}\r\n'.encode(),
            (f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{file_path.name}"\r\n'
             f"Content-Type: {content_type}\r\n\r\n").encode() + file_path.read_bytes() + b"\r\n",
            f"--{boundary}--\r\n".encode(),
        ]
        return self.call("POST", path, raw=b"".join(parts), content_type=f"multipart/form-data; boundary={boundary}")


def read_env() -> Dict[str, str]:
    values: Dict[str, str] = {}
    env_file = ROOT / ".env"
    if env_file.exists():
        for line in env_file.read_text().splitlines():
            if "=" in line and not line.lstrip().startswith("#"):
                key, value = line.split("=", 1)
                values[key.strip()] = value.strip()
    values.update({key: value for key, value in os.environ.items() if key in ("ADMIN_EMAIL", "ADMIN_PASSWORD", "API_PORT")})
    return values


def step(message: str) -> None:
    print(f"\n==> {message}")


def ensure_project_lead(api: Api) -> str:
    number, email = "AC-PL-001", "lena.novak@astercare.example"
    found = api.call("GET", "/employees?" + urllib.parse.urlencode({"search": number, "limit": 5}))["items"]
    if found:
        return found[0]["id"]
    created = api.call("POST", "/employees", {
        "employeeNumber": number, "firstName": "Lena", "lastName": "Novak", "workEmail": email, "phone": None})
    print(f"    created fictional project lead Lena Novak ({number})")
    return created["id"]


def ensure_project(api: Api, lead_id: str) -> str:
    for project in api.call("GET", "/projects"):
        if project["code"] == PROJECT["code"]:
            print(f"    project {PROJECT['name']} already exists")
            return project["id"]
    created = api.call("POST", "/projects", {
        "code": PROJECT["code"], "name": PROJECT["name"], "purpose": PROJECT["purpose"],
        "description": PROJECT["description"], "departmentId": None, "leadEmployeeId": lead_id,
        "onboardingContactEmployeeId": lead_id, "status": "Active", "links": []})
    print(f"    created project {PROJECT['name']} ({PROJECT['code']})")
    return created["id"]


def ensure_article(api: Api, spec: Dict[str, Any], project_id: Optional[str]) -> str:
    query = {"includeDrafts": "true", "search": spec["title"]}
    if project_id:
        query["projectId"] = project_id
    for article in api.call("GET", "/knowledge/articles?" + urllib.parse.urlencode(query)):
        if article["slug"] == spec["slug"]:
            if article["status"] != "Published":
                api.call("POST", f"/knowledge/articles/{article['id']}/publish", {}, etag=article["version"])
            return article["id"]
    summary = " ".join(spec["sections"][0][1])
    article = api.call("POST", "/knowledge/articles", {
        "slug": spec["slug"], "title": spec["title"], "description": spec["title"], "category": spec["category"],
        "content": summary, "audience": "ProjectMembers" if project_id else "AllEmployees",
        "projectId": project_id, "departmentId": None, "sortOrder": 0})
    api.call("POST", f"/knowledge/articles/{article['id']}/publish", {}, etag=article["version"])
    print(f"    created and published article: {spec['title']}")
    return article["id"]


def ensure_document(api: Api, article_id: str, spec: Dict[str, Any], folder: Path) -> str:
    existing = api.call("GET", f"/knowledge/articles/{article_id}/documents")
    if existing:
        return existing[0]["id"]
    file_path = folder / f"{spec['slug']}.{spec['format']}"
    writer = write_pdf if spec["format"] == "pdf" else write_docx
    writer(str(file_path), spec["title"], DISCLAIMER, spec["sections"])
    uploaded = api.upload(f"/knowledge/articles/{article_id}/documents", file_path, CONTENT_TYPES[spec["format"]], "Onboarding")
    print(f"    uploaded {file_path.name}")
    return uploaded["id"]


def wait_until_clean(api: Api, documents: List[Dict[str, str]], timeout_seconds: int = 600) -> None:
    deadline = time.time() + timeout_seconds
    pending = list(documents)
    while pending and time.time() < deadline:
        still = []
        for doc in pending:
            status = next(d["status"] for d in api.call("GET", f"/knowledge/articles/{doc['article']}/documents") if d["id"] == doc["id"])
            if status == "Clean":
                continue
            if status in ("Rejected", "Infected", "Archived"):
                raise SystemExit(f"Document {doc['title']} was {status}; check the worker logs.")
            still.append(doc)
        pending = still
        if pending:
            print(f"    waiting for virus scan of {len(pending)} document(s)...")
            time.sleep(5)
    if pending:
        raise SystemExit("Timed out waiting for the virus scan. Is the worker running?  docker compose ps")


def approve_for_ai(api: Api, documents: List[Dict[str, str]]) -> None:
    for doc in documents:
        try:
            api.call("POST", f"/knowledge/articles/{doc['article']}/documents/{doc['id']}/ai-approval", {"classification": "NormalInternal"})
        except ApiError as error:
            print(f"    could not approve {doc['title']} for AI: {error}")


def reading_steps(documents: List[Dict[str, str]], extra_title: str, extra_instructions: str) -> List[Dict[str, Any]]:
    steps = [{
        "sectionTitle": doc["category"], "sectionOrder": index, "title": doc["title"],
        "instructions": f'Read "{doc["title"]}" carefully. The onboarding quiz will ask about it.',
        "expectedOutcome": f'You understand the key rules in "{doc["title"]}".',
        "stepType": "ReadDocument", "stepOrder": index, "dueAfterDays": 3 + index, "isRequired": True,
        "documentId": doc["id"], "minimumReadingMinutes": 2,
    } for index, doc in enumerate(documents)]
    steps.append({
        "sectionTitle": "Getting started", "sectionOrder": len(steps), "title": extra_title, "instructions": extra_instructions,
        "expectedOutcome": "You know who to ask for help.", "stepType": "PracticalTask", "stepOrder": len(steps),
        "dueAfterDays": 5, "isRequired": True,
    })
    return steps


def ensure_package(api: Api, name: str, kind: str, project_id: Optional[str], steps: List[Dict[str, Any]], description: str) -> Dict[str, Any]:
    for template in api.call("GET", "/onboarding/templates?publishedOnly=false"):
        if template["name"] == name:
            print(f"    package already exists: {name}")
            return template
    template = api.call("POST", "/onboarding/templates", {
        "name": name, "description": description, "durationDays": 30, "kind": kind, "projectId": project_id, "steps": steps})
    template = api.call("POST", f"/onboarding/templates/{template['id']}/publish", {}, etag=template["version"])
    print(f"    created and published package: {name}")
    return template


def ensure_quiz(api: Api, template: Dict[str, Any], title: str, question_count: int) -> None:
    try:
        quiz = api.call("GET", f"/onboarding/templates/{template['id']}/quiz")
    except ApiError as error:
        if error.status != 404:
            raise
        quiz = None
    if not quiz:
        quiz = api.call("POST", f"/onboarding/templates/{template['id']}/quiz",
                        {"title": title, "passingPercentage": 80, "questionCount": question_count})
        print(f"    created draft quiz: {title}")
    if quiz["status"] != "Draft" or quiz["questions"] or quiz.get("generationStatus") in ("Queued", "Running"):
        return
    try:
        api.call("POST", f"/onboarding/quizzes/{quiz['id']}/generate")
        print("    AI question generation queued; the worker will fill the draft in a few minutes")
    except ApiError as error:
        print(f"    AI generation not started yet: {error}")
        print("    -> add OPENAI_API_KEY to .env, restart api and worker, wait for indexing, then click 'Generate with AI'")


def main() -> None:
    env = read_env()
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--api", default=f"http://127.0.0.1:{env.get('API_PORT', '8080')}")
    args = parser.parse_args()
    if not env.get("ADMIN_EMAIL") or not env.get("ADMIN_PASSWORD"):
        raise SystemExit("ADMIN_EMAIL and ADMIN_PASSWORD must be set in .env (the HR administrator account).")

    api = Api(args.api)
    step(f"Signing in to {args.api} as the HR administrator from .env")
    api.login(env["ADMIN_EMAIL"], env["ADMIN_PASSWORD"])

    step(f"Project {PROJECT['name']}")
    project_id = ensure_project(api, ensure_project_lead(api))

    folder = Path(tempfile.mkdtemp(prefix="astercare-demo-"))
    company_docs, project_docs = [], []
    for specs, target, owner in ((COMPANY_DOCUMENTS, company_docs, None), (PROJECT_DOCUMENTS, project_docs, project_id)):
        step("Company documents" if owner is None else f"{PROJECT['name']} documents")
        for spec in specs:
            article_id = ensure_article(api, spec, owner)
            target.append({"article": article_id, "id": ensure_document(api, article_id, spec, folder),
                           "title": spec["title"], "category": spec["category"]})

    step("Virus scan")
    wait_until_clean(api, company_docs + project_docs)
    step("Approving documents for AI (classification: NormalInternal)")
    approve_for_ai(api, company_docs + project_docs)

    step("Onboarding packages")
    company = ensure_package(api, COMPANY_PACKAGE, "Company", None,
                             reading_steps(company_docs, "Meet your onboarding buddy",
                                           "Book 30 minutes with your onboarding buddy in your first week."),
                             f"How {COMPANY} works: quality, security, conduct, and safety.")
    # Deliberately not marked "default for new employees": HR assigns it by hand and picks the documents per person.
    project = ensure_package(api, PROJECT_PACKAGE, "Project", project_id,
                             reading_steps(project_docs, "Pair with an engineer",
                                           "Pair with an experienced Pulse One engineer for your first sprint."),
                             f"What new {PROJECT['name']} team members need to know.")

    step("Quizzes")
    ensure_quiz(api, company, f"{COMPANY} company quiz", 5)
    ensure_quiz(api, project, f"{PROJECT['name']} project quiz", 4)

    print(f"""
Done. Open the app and sign in as HR:
  * Onboarding -> Manage: review the AI questions on each package (Assessment), then publish each quiz.
  * People: create the new employee, set their employment to Active, and create their user account (Employee role).
  * Projects: add them to {PROJECT['name']} so the project documents can be included.
  * Onboarding -> Manage -> Assign journey: choose "{COMPANY_PACKAGE}" and the {PROJECT['name']} project,
    untick the documents this employee does not need, and assign. Their quiz only asks about the ticked documents.
Generated files: {folder}""")


if __name__ == "__main__":
    main()
