import importlib.util
for m in ['pypdf', 'PyPDF2', 'pdfplumber', 'fitz', 'pdfminer']:
    print(f"{m}: {'yes' if importlib.util.find_spec(m) else 'no'}")
