"""Extrai imagens incorporadas, sem recorte, alteração de cor ou recriação."""
from pathlib import Path
from pypdf import PdfReader

output = Path('/private/tmp/museum-catalog-images')
output.mkdir(exist_ok=True)
for prefix, path, pages in [
    ('anita', '/private/tmp/museum-anita-catalog.pdf', [67]),
    ('di', '/private/tmp/museum-di-catalog.pdf', [61, 83, 84, 85, 89, 101]),
]:
    reader = PdfReader(path)
    for page_number in pages:
        for item in reader.pages[page_number - 1].images:
            target = output / f'{prefix}-{page_number}-{item.name}'
            target.write_bytes(item.data)
            print(target)
