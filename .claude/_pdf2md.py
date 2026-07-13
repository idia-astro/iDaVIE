import fitz
from pathlib import Path

src_dir = Path(r"docs\sub-team-6\deliverables")
pdfs = list(src_dir.glob("*.pdf"))
for pdf in pdfs:
    doc = fitz.open(pdf)
    parts = [f"# {pdf.stem}\n", f"_Source: `{pdf.name}` ({doc.page_count} pages)_\n"]
    for i, page in enumerate(doc, 1):
        parts.append(f"\n---\n\n## Page {i}\n")
        parts.append(page.get_text("text"))
    out = pdf.with_suffix(".md")
    out.write_text("\n".join(parts), encoding="utf-8")
    print(f"wrote: {out} ({doc.page_count} pages)")
