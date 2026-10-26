"""Gera a arte tipográfica do splash a partir da autoria usada no menu."""
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
source = (root / "UnityProject/Assets/Scripts/UI/WelcomeScreen.cs").read_text(encoding="utf-8")
credit = re.search(r'public const string Authorship = "([^"]+)";', source).group(1)
label, names = credit.split(": ", 1)
lines = [label + ":", *names.split(" | ")]
assert len(lines) == 3 and all(lines), "A autoria deve conter dois nomes completos."

image = Image.new("RGBA", (1600, 420))
draw = ImageDraw.Draw(image)
for text, top, size in zip(lines, (40, 145, 250), (48, 76, 76)):
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", size)
    box = draw.textbbox((800, top), text, font=font, anchor="mt")
    assert 40 <= box[0] < box[2] <= 1560 and 0 <= box[1] < box[3] <= 400, (text, box)
    draw.text((800, top), text, font=font, anchor="mt", fill=(247, 240, 219, 255))

output = root / "UnityProject/Assets/Branding/MuseumAuthorsSplash.png"
image.save(output)
with Image.open(output) as saved:
    assert saved.format == "PNG" and saved.mode == "RGBA" and saved.size == (1600, 420)
    assert saved.getchannel("A").getextrema() == (0, 255)
print("Arte gerada e verificada:", output)
