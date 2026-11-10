"""Synthetic onboarding documents for a fictional medical-device company.

Everything here is invented for demonstration and testing. "AsterCare Medical" and "Pulse One" are fictional; the
rules are plausible internal policies, not legal or regulatory advice. Standards (ISO 13485, ISO 14971, IEC 62304)
are only named as the frameworks a company like this would follow.
"""

COMPANY = "AsterCare Medical"
DISCLAIMER = "Fictional company. Synthetic content for onboarding demonstrations only."

PROJECT = {
    "code": "PULSE1",
    "name": "Pulse One",
    "purpose": "Wearable single-lead ECG patch for multi-day heart-rhythm monitoring",
    "description": "Pulse One combines a 7-day adhesive ECG patch, a companion mobile app, and cloud rhythm analysis "
    "that flags possible atrial fibrillation for review by a clinician.",
}

# Each document: slug, title, category, file format, sections of (heading, paragraphs).
COMPANY_DOCUMENTS = [
    {
        "slug": "astercare-company-handbook",
        "title": "AsterCare Company Handbook",
        "category": "How we work",
        "format": "pdf",
        "sections": [
            ("Who we are", [
                "AsterCare Medical designs and manufactures non-invasive cardiac monitoring devices. Our mission is to "
                "help clinicians detect heart-rhythm problems earlier, outside the hospital.",
                "We have four departments: Research and Development, Quality and Regulatory, Manufacturing, and "
                "Clinical and Market Access. Every new employee is given an onboarding buddy from their own department "
                "for the first 30 days.",
            ]),
            ("Working hours and leave", [
                "Core hours are 10:00 to 15:00, Monday to Friday. Outside core hours you can plan your own time with "
                "your manager.",
                "Holiday requests are made in the HR system at least 10 working days before the first day of leave. "
                "Sick leave must be reported to your manager before 09:30 on the first day of absence.",
            ]),
            ("Communication and escalation", [
                "Day-to-day questions go to your team channel. Anything that could affect patient safety or product "
                "quality is escalated the same day to your manager and to Quality and Regulatory.",
                "The company all-hands meeting is held on the first Monday of every month. Recordings are kept for "
                "90 days.",
            ]),
            ("Our values", [
                "Patient first: when in doubt, choose the option that is safest for the patient. "
                "Speak up: raising a concern is always welcome and never punished. "
                "Document it: if it is not written down in a controlled system, it did not happen.",
            ]),
        ],
    },
    {
        "slug": "astercare-quality-management-essentials",
        "title": "Quality Management System Essentials",
        "category": "Quality",
        "format": "pdf",
        "sections": [
            ("Why the QMS matters", [
                "AsterCare runs a quality management system based on ISO 13485. It makes sure every device we ship is "
                "designed, built, and tested the same controlled way, and that we can prove it.",
            ]),
            ("Controlled documents", [
                "Only use procedures and work instructions from the electronic QMS (eQMS). Printed copies are "
                "uncontrolled and must be destroyed at the end of the working day.",
                "You may not perform a task covered by a procedure until your training record for that procedure is "
                "signed in the eQMS.",
            ]),
            ("Deviations and CAPA", [
                "A deviation is any departure from an approved procedure or specification. Report every deviation in "
                "the eQMS within 24 hours of discovering it, even if it looks minor.",
                "When a deviation needs a corrective and preventive action (CAPA), Quality assigns a CAPA owner within "
                "5 working days. The owner finds the root cause before choosing a correction.",
            ]),
            ("Complaints", [
                "Any information suggesting a device did not perform as intended is a complaint. Forward it to the "
                "Complaint Handling team within 1 working day, including the device serial number if known. Never try "
                "to resolve a complaint yourself.",
            ]),
        ],
    },
    {
        "slug": "astercare-information-security",
        "title": "Information Security and Patient Data",
        "category": "Security",
        "format": "docx",
        "sections": [
            ("Data we protect", [
                "We classify information as Public, Internal, Confidential, or Patient Data. Patient Data includes any "
                "ECG recording or health information linked to a person, and it is the most protected class.",
                "Patient Data may only be stored and processed in approved clinical systems. Never copy it to e-mail, "
                "chat, personal drives, or USB sticks.",
            ]),
            ("Everyday rules", [
                "Multi-factor authentication is required for every company account. Lock your screen whenever you "
                "leave your desk; laptops lock automatically after 5 minutes.",
                "USB storage devices are blocked on company laptops. Use the approved file-sharing system instead.",
            ]),
            ("Incidents and phishing", [
                "Report a suspected security incident to the Security Desk (extension 4400) within 1 hour. Examples "
                "are a lost laptop, a password typed into a suspicious website, or Patient Data sent to the wrong person.",
                "Report phishing e-mails with the Report Phishing button. Do not forward them to colleagues.",
            ]),
        ],
    },
    {
        "slug": "astercare-code-of-conduct",
        "title": "Code of Conduct and Interactions with Healthcare Professionals",
        "category": "Compliance",
        "format": "pdf",
        "sections": [
            ("Integrity", [
                "We compete on the quality of our devices, never on favours. Bribery and kickbacks are forbidden, "
                "whatever the local custom.",
            ]),
            ("Healthcare professionals", [
                "Record every paid interaction with a healthcare professional, such as consulting or speaking fees, in "
                "the Compliance Register within 5 working days.",
                "Gifts to healthcare professionals are not allowed. Modest meals during a scientific meeting are "
                "allowed up to 50 EUR per person and must also be recorded.",
                "Only promote our devices for their approved intended use. Questions about other uses are passed to "
                "Medical Affairs.",
            ]),
            ("Conflicts of interest and speaking up", [
                "Declare possible conflicts of interest when you join and again every year in January.",
                "Concerns can be raised with your manager, with Compliance, or anonymously through the Speak Up line, "
                "which is available 24 hours a day.",
            ]),
        ],
    },
    {
        "slug": "astercare-health-safety-cleanroom",
        "title": "Health, Safety and Cleanroom Rules",
        "category": "Safety",
        "format": "docx",
        "sections": [
            ("General safety", [
                "If the fire alarm sounds, leave by the nearest exit and go to Assembly Point B in the car park. Do not "
                "use the lifts. Wait for the fire warden to confirm everyone is present.",
                "Report every accident or near miss to your supervisor before the end of the same shift.",
            ]),
            ("Electrostatic discharge", [
                "Electronics assembly areas are ESD-protected. Wear a tested wrist strap and ESD shoes before touching "
                "any circuit board. Test your wrist strap at the station at the start of each shift.",
            ]),
            ("Cleanroom gowning", [
                "Gown in this order: hair cover, face mask, coverall, overshoes, and gloves last. Remove jewellery and "
                "watches before entering.",
                "Never bring paper, food, or drink into the cleanroom. Leave immediately if your gown is damaged.",
            ]),
        ],
    },
]

PROJECT_DOCUMENTS = [
    {
        "slug": "pulse-one-product-overview",
        "title": "Pulse One Product Overview and Intended Use",
        "category": "Product",
        "format": "pdf",
        "sections": [
            ("What Pulse One is", [
                "Pulse One is a single-lead ECG patch worn on the chest for up to 7 days. It records continuously and "
                "uploads data through the Pulse One mobile app to cloud analysis.",
                "The system has three parts: the adhesive patch with its sensor module, the companion mobile app, and "
                "the cloud rhythm-analysis service.",
            ]),
            ("Intended use", [
                "Pulse One is intended for ambulatory monitoring of adult patients to help clinicians detect atrial "
                "fibrillation episodes. Results are reviewed by a qualified clinician before any diagnosis.",
                "Pulse One is not a life-support or alarm device. It must not be used for real-time monitoring of "
                "critically ill patients, and it does not raise emergency alerts.",
            ]),
            ("Device classification", [
                "For the EU market the project treats Pulse One as a Class IIa medical device. Any change to the "
                "intended use must be approved by Regulatory before work starts.",
            ]),
        ],
    },
    {
        "slug": "pulse-one-software-development",
        "title": "Pulse One Software Development Process",
        "category": "Engineering",
        "format": "pdf",
        "sections": [
            ("Framework", [
                "Pulse One software follows IEC 62304. The rhythm-analysis module is software safety class B.",
            ]),
            ("Changes and reviews", [
                "Every change goes through a pull request. Pull requests need two approvals; for safety-related code, "
                "one approver must be from Quality Assurance.",
                "Each commit references a requirement ID in the form PO-REQ-123 so the change can be traced to its "
                "requirement and test.",
            ]),
            ("Testing and release", [
                "Unit-test coverage for the rhythm-analysis module must stay at or above 80 percent. A build that drops "
                "below this cannot be merged.",
                "Third-party libraries (SOUP) must be listed in the SOUP register with their version before they are "
                "used. Releases are approved only by the Release Board, which meets every second Thursday.",
            ]),
        ],
    },
    {
        "slug": "pulse-one-risk-management",
        "title": "Pulse One Risk Management Plan",
        "category": "Risk",
        "format": "docx",
        "sections": [
            ("Approach", [
                "Risk management for Pulse One follows ISO 14971. All hazards, causes, and controls are kept in the "
                "hazard log in the eQMS; spreadsheets are not used.",
            ]),
            ("Reporting a new hazard", [
                "Anyone can identify a hazard. Report a new or changed hazard to the risk owner within 2 working days, "
                "with a short description of the situation that could cause harm.",
            ]),
            ("Key hazards", [
                "The three key hazards are skin irritation from the adhesive, a missed atrial fibrillation episode, "
                "and loss of recorded data before upload.",
                "Each key hazard has a verified control: a biocompatibility-tested adhesive, clinician review of every "
                "report, and on-patch storage of the full 7 days of data.",
            ]),
        ],
    },
    {
        "slug": "pulse-one-team-ways-of-working",
        "title": "Pulse One Team and Ways of Working",
        "category": "Team",
        "format": "pdf",
        "sections": [
            ("Rhythm of the team", [
                "The team works in 2-week sprints. The daily stand-up is at 09:30 and lasts at most 15 minutes. Sprint "
                "review and retrospective are on the last Wednesday of each sprint.",
            ]),
            ("Definition of done", [
                "A task is done when the code is merged, tests pass, the requirement trace is updated, and any "
                "affected hazard in the hazard log has been reviewed.",
            ]),
            ("Who to ask", [
                "The project lead owns scope and priorities. The QA lead approves safety-related changes and test "
                "plans. Regulatory questions go to the regulatory specialist assigned to the project.",
                "New team members pair with an experienced engineer for their first two sprints.",
            ]),
        ],
    },
]
