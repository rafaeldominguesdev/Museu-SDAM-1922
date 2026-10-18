"""Package verified source files for Unity; preserve composition and pixel dimensions."""
import hashlib
import io
import json
from pathlib import Path
import shutil

from PIL import Image, ImageCms

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
UNITY = PROJECT / "UnityProject"
source_manifests = [HERE / "anita/manifest.json", HERE / "gallery/manifest.json"]
items = [item for manifest in source_manifests
         for item in json.loads(manifest.read_text(encoding="utf-8"))["items"]]
assert len(items) == 9 and len({item["exhibitId"] for item in items}) == 9
assert all(Path(item["filepath"]).is_file() for item in items)
assert all(isinstance(item["year"], int) for item in items)
destination = UNITY / "Assets/Artworks/Images"
destination.mkdir(parents=True, exist_ok=True)
catalog_path = UNITY / "Assets/Resources/MuseumCatalog.json"
catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
catalog_by_id = {item["uniqueId"]: item for item in catalog["items"]}
relationships = {
    "homem_amarelo": "Anita Malfatti participou da exposição de 1922. Esta obra anterior à Semana ajuda a compreender os debates que antecederam o evento.",
    "estudante_russa": "Anita Malfatti participou da Semana. Este retrato pertence à pesquisa expressiva da artista anterior a 1922.",
    "tropical": "Pintura anterior à Semana, apresentada como contexto da trajetória de Anita Malfatti, uma de suas participantes.",
    "di_cavalcanti_pierrot": "Di Cavalcanti participou da organização da Semana e criou a capa de seu catálogo. O MASP registra esta obra como sem data; sua presença aqui contextualiza a produção do artista, sem afirmar que foi exposta em 1922.",
    "john_graz": "Segundo o MASP, este retrato de 1917 foi exposto na Semana de Arte Moderna de 1922.",
    "lasar_segall": "Lasar Segall não participou da Semana de 1922. A obra de 1919 amplia o contexto expressionista e os caminhos do modernismo além do evento.",
    "vicente_rego_monteiro": "Vicente do Rego Monteiro esteve representado na Semana. Esta pintura é de 1923, posterior ao evento, e apresenta um desdobramento de sua pesquisa.",
    "zina_aita": "Zina Aita participou da exposição de 1922. A pintura pertence ao mesmo ano; esta ficha não afirma que o exemplar foi apresentado durante a Semana.",
    "ferrignac": "Ferrignac foi expositor da Semana. A fonte registra Colombina como sem data; esta reprodução apresenta a produção do artista sem atribuir sua execução ou exposição a 1922."
}
packaged = []
srgb = ImageCms.createProfile("sRGB")
for item in items:
    source = Path(item["filepath"])
    with Image.open(source) as original:
        width, height = original.size
        profile = original.info.get("icc_profile")
        target = destination / (item["exhibitId"] + source.suffix.lower())
        conversion = "Arquivo original, sem recorte ou redimensionamento."
        if profile:
            # Color-profile transcoding is needed for Unity, which samples sRGB.
            # It changes no composition, detail or dimensions of the reproduction.
            source_profile = ImageCms.ImageCmsProfile(io.BytesIO(profile))
            converted = ImageCms.profileToProfile(original, source_profile, srgb, outputMode="RGB")
            target = target.with_suffix(".jpg") if target.suffix == ".jpeg" else target
            converted.save(target, quality=96, subsampling=0,
                           icc_profile=ImageCms.ImageCmsProfile(srgb).tobytes())
            conversion = "Perfil ICC convertido para sRGB; dimensões e composição preservadas."
        elif original.mode == "CMYK":
            raise RuntimeError(f"CMYK source without a color profile: {source}")
        else:
            shutil.copy2(source, target)
    record = {key: value for key, value in item.items() if key != "filepath"}
    record.update(texturePath=str(target.relative_to(UNITY)), width=width, height=height,
                  preparation=conversion, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  textureSha256=hashlib.sha256(target.read_bytes()).hexdigest())
    packaged.append(record)
    entry = catalog_by_id[item["exhibitId"]]
    for key in ("title", "artist", "year", "periodo", "tecnica", "description"):
        entry[key] = item[key]
    entry["categoria"] = "Pintura" if item["exhibitId"] not in ("di_cavalcanti_pierrot", "ferrignac") else "Obra sobre papel"
    entry["resumo"] = item["description"].split(". ")[0].rstrip(".") + "."
    entry["relacao_com_a_semana_de_1922"] = relationships[item["exhibitId"]]
    entry["obra_historica_ou_reinterpretacao"] = "Reprodução digital da obra histórica identificada nesta ficha. A imagem preserva a composição e as proporções da fonte. Crédito da reprodução: " + item.get("creditFull", item["credit"]).rstrip(".") + "."
    entry["fontes"] = "\n".join([item["sourcePage"], *item.get("verificationSources", []), "Imagem: " + item["imageUrl"]])
    entry["funFact"] = ""
catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
(UNITY / "Assets/Resources/MuseumArtworkImages.json").write_text(
    json.dumps({"items": packaged}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps([{"id": x["exhibitId"], "pixels": [x["width"], x["height"]],
                   "path": x["texturePath"]} for x in packaged], ensure_ascii=False, indent=2))
