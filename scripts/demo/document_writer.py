"""Minimal, dependency-free PDF and DOCX writers for synthetic demo documents.

The output is plain text with headings: enough for the app's text extraction (PdfPig / Open XML) and for people to read.
"""

from __future__ import annotations

import textwrap
import zipfile
from typing import List, Optional, Tuple
from xml.sax.saxutils import escape

Section = Tuple[str, List[str]]


def write_pdf(path: str, title: str, subtitle: str, sections: list[Section]) -> None:
    """Writes an A4 PDF using the built-in Helvetica fonts, wrapping text and adding pages as needed."""
    lines: list[tuple[str, int, str]] = [("F2", 18, title), ("F1", 9, subtitle), ("F1", 11, "")]
    for heading, paragraphs in sections:
        lines.append(("F2", 13, heading))
        for paragraph in paragraphs:
            lines.extend(("F1", 11, line) for line in textwrap.wrap(paragraph, 90))
            lines.append(("F1", 11, ""))

    pages: list[list[tuple[str, int, str]]] = [[]]
    y = 800
    for font, size, text in lines:
        step = size + 6
        if y - step < 60:
            pages.append([])
            y = 800
        pages[-1].append((font, size, text))
        y -= step

    objects: list[bytes] = []

    def add(body: bytes) -> int:
        objects.append(body)
        return len(objects)

    catalog = add(b"")  # filled in below
    pages_id = add(b"")
    font_regular = add(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
    font_bold = add(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
    page_ids = []
    for page in pages:
        y = 800
        commands = ["BT"]
        for font, size, text in page:
            # Trailing space: text extractors join lines directly, so without it words merge at line breaks.
            commands.append(f"/{font} {size} Tf 1 0 0 1 56 {y} Tm ({_pdf_text(text + ' ')}) Tj")
            y -= size + 6
        commands.append("ET")
        stream = "\n".join(commands).encode("latin-1")
        content = add(b"<< /Length %d >>\nstream\n" % len(stream) + stream + b"\nendstream")
        page_ids.append(add(
            b"<< /Type /Page /Parent %d 0 R /MediaBox [0 0 595 842] /Contents %d 0 R "
            b"/Resources << /Font << /F1 %d 0 R /F2 %d 0 R >> >> >>" % (pages_id, content, font_regular, font_bold)))
    objects[catalog - 1] = b"<< /Type /Catalog /Pages %d 0 R >>" % pages_id
    kids = " ".join(f"{page} 0 R" for page in page_ids).encode()
    objects[pages_id - 1] = b"<< /Type /Pages /Kids [" + kids + b"] /Count %d >>" % len(page_ids)

    output = bytearray(b"%PDF-1.4\n%\xe2\xe3\xcf\xd3\n")
    offsets = []
    for number, body in enumerate(objects, start=1):
        offsets.append(len(output))
        output += b"%d 0 obj\n" % number + body + b"\nendobj\n"
    xref = len(output)
    output += b"xref\n0 %d\n0000000000 65535 f \n" % (len(objects) + 1)
    output += b"".join(b"%010d 00000 n \n" % offset for offset in offsets)
    output += b"trailer\n<< /Size %d /Root %d 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (len(objects) + 1, catalog, xref)
    with open(path, "wb") as file:
        file.write(output)


def write_docx(path: str, title: str, subtitle: str, sections: list[Section]) -> None:
    """Writes a minimal Word document with a title, headings, and paragraphs."""
    def paragraph(text: str, style: Optional[str] = None) -> str:
        properties = f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>' if style else ""
        return f'<w:p>{properties}<w:r><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>'

    body = [paragraph(title, "Title"), paragraph(subtitle)]
    for heading, paragraphs in sections:
        body.append(paragraph(heading, "Heading1"))
        body.extend(paragraph(text) for text in paragraphs)
    document = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
        f'<w:body>{"".join(body)}<w:sectPr/></w:body></w:document>')
    content_types = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Override PartName="/word/document.xml" '
        'ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>')
    relationships = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" '
        'Target="word/document.xml"/></Relationships>')
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("[Content_Types].xml", content_types)
        archive.writestr("_rels/.rels", relationships)
        archive.writestr("word/document.xml", document)


def _pdf_text(text: str) -> str:
    encoded = text.encode("latin-1", "replace").decode("latin-1")
    return encoded.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")
