"""Import the complete Unity museum export into Blender 5.1.

Run: Blender --background --factory-startup --python import_museum.py
Add -- --render to render preview.png. No source Unity files are changed.
"""

import argparse
from array import array
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Matrix, Vector


HERE = Path(__file__).resolve().parent
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
parser = argparse.ArgumentParser()
parser.add_argument("--source", type=Path, default=HERE / "museum-export.json")
parser.add_argument("--output", type=Path, default=HERE / "Museu_Atualizado.blend")
parser.add_argument("--render", action="store_true")
parser.add_argument("--camera", choices=("hall", "gallery", "artworks"), default="hall")
args = parser.parse_args(argv)
source = args.source.resolve()
out = args.output.resolve()
out.parent.mkdir(parents=True, exist_ok=True)
payload = json.loads(source.read_text(encoding="utf-8-sig"))
warnings = []


def srgb(value):
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def rgba(value):
    return (*[srgb(max(0.0, float(v))) for v in value[:3]],
            float(value[3]) if len(value) > 3 else 1.0)


def vecs(values, size):
    return [values[i:i + size] for i in range(0, len(values), size)]


def collection(name, parent=None):
    result = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(result)
    return result


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for old in list(bpy.data.collections):
    bpy.data.collections.remove(old)
scene = bpy.context.scene
scene.name = "Museu | Semana de Arte Moderna"
geometry = collection("01 | Museu — geometria completa")
captions = collection("02 | Inscrições e legendas")
lighting = collection("03 | Iluminação")
original_lights = collection("Luzes da cena Unity", lighting)
preview_lights = collection("Luz ambiente de apresentação", lighting)
cameras = collection("04 | Câmeras de visita")
groups = {}


def geometry_collection(record):
    # The first two hierarchy levels make editing pleasant without hundreds
    # of empty one-object collections. Original full paths remain on objects.
    parts = [x for x in record.get("path", "").split("/") if x][:-1][:2]
    parent = geometry
    for count, name in enumerate(parts):
        key = tuple(parts[:count + 1])
        if key not in groups:
            groups[key] = collection(name, parent)
        parent = groups[key]
    return parent


material_map = {}
image_cache = {}
contact_ids = set()


def make_material(record):
    material = bpy.data.materials.new(record.get("name", "Material"))
    material.use_nodes = True
    material["Unity material ID"] = record["id"]
    material["Unity shader"] = record.get("shader", "")
    color = rgba(record.get("color", [1, 1, 1, 1]))
    material.diffuse_color = color
    material.roughness = max(0.04, min(1.0, record.get("roughness", 0.65)))
    material.metallic = max(0.0, min(1.0, record.get("metallic", 0.0)))
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (650, 80)
    shader = record.get("shader", "").lower().replace(" ", "")
    if "contactshadow" in shader:
        contact_ids.add(record["id"])
        attribute = nodes.new("ShaderNodeVertexColor")
        attribute.layer_name = "UnityColor"
        attribute.location = (-500, 50)
        lightness = nodes.new("ShaderNodeRGBToBW")
        lightness.location = (-280, 50)
        links.new(attribute.outputs["Color"], lightness.inputs[0])
        transparent = nodes.new("ShaderNodeBsdfTransparent")
        transparent.location = (-100, -100)
        black = nodes.new("ShaderNodeEmission")
        black.inputs["Color"].default_value = (0, 0, 0, 1)
        black.inputs["Strength"].default_value = 1
        black.location = (-100, 140)
        mix = nodes.new("ShaderNodeMixShader")
        mix.location = (350, 70)
        links.new(lightness.outputs[0], mix.inputs[0])
        links.new(black.outputs[0], mix.inputs[1])
        links.new(transparent.outputs[0], mix.inputs[2])
        links.new(mix.outputs[0], output.inputs["Surface"])
        material.surface_render_method = "DITHERED"
        material.diffuse_color = (0.12, 0.10, 0.07, 0.25)
        return material

    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (320, 80)
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = material.metallic
    bsdf.inputs["Roughness"].default_value = material.roughness
    bsdf.inputs["Alpha"].default_value = color[3]
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    texture_name = record.get("texture", "")
    base_color_socket = None
    if texture_name:
        texture_path = source.parent / texture_name
        if texture_path.is_file():
            key = str(texture_path.resolve())
            if key not in image_cache:
                image_cache[key] = bpy.data.images.load(key, check_existing=True)
                image_cache[key].pack()
            texture = nodes.new("ShaderNodeTexImage")
            texture.image = image_cache[key]
            texture.interpolation = "Linear"
            texture.extension = "REPEAT"
            texture.location = (-410, 140)
            coordinates = nodes.new("ShaderNodeTexCoord")
            coordinates.location = (-900, 140)
            mapping = nodes.new("ShaderNodeMapping")
            mapping.location = (-700, 140)
            scale = record.get("textureScale", [1, 1])
            offset = record.get("textureOffset", [0, 0])
            mapping.inputs["Scale"].default_value = (*scale, 1)
            mapping.inputs["Location"].default_value = (*offset, 0)
            links.new(coordinates.outputs["UV"], mapping.inputs["Vector"])
            links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
            multiply = nodes.new("ShaderNodeMixRGB")
            multiply.blend_type = "MULTIPLY"
            multiply.inputs[0].default_value = 1
            multiply.inputs[2].default_value = color
            multiply.location = (-50, 140)
            links.new(texture.outputs["Color"], multiply.inputs[1])
            base_color_socket = multiply.outputs[0]
            links.new(base_color_socket, bsdf.inputs["Base Color"])
            if color[3] < 0.999 or "transparent" in shader:
                alpha = nodes.new("ShaderNodeMath")
                alpha.operation = "MULTIPLY"
                alpha.inputs[1].default_value = color[3]
                links.new(texture.outputs["Alpha"], alpha.inputs[0])
                links.new(alpha.outputs[0], bsdf.inputs["Alpha"])
        else:
            warnings.append(f"Texture missing: {texture_path}")

    emission = record.get("emission", [0, 0, 0])
    if max(emission, default=0) > 0:
        bsdf.inputs["Emission Color"].default_value = (*emission[:3], 1)
        bsdf.inputs["Emission Strength"].default_value = 1
        if record.get("emissionUsesTexture") and base_color_socket:
            glow = nodes.new("ShaderNodeMixRGB")
            glow.blend_type = "MULTIPLY"
            glow.inputs[0].default_value = 1
            glow.inputs[2].default_value = (*emission[:3], 1)
            links.new(base_color_socket, glow.inputs[1])
            links.new(glow.outputs[0], bsdf.inputs["Emission Color"])
    else:
        # Unity's Gamma ambient and baked/probe light have no direct export.
        # A small diffuse lift keeps enclosed rooms legible in EEVEE.
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = 0.08
        if base_color_socket:
            links.new(base_color_socket, bsdf.inputs["Emission Color"])
    if color[3] < 0.999:
        material.surface_render_method = "DITHERED"
    return material


for record in payload.get("materials", []):
    material_map[record["id"]] = make_material(record)
material_map[-1] = make_material({"id": -1, "name": "Material sem atribuição",
                                "color": [0.72, 0.72, 0.72, 1]})

expected_triangles = 0
for number, record in enumerate(payload.get("meshes", [])):
    vertices = record["vertices"]
    indices = record["triangles"]
    if len(vertices) % 3 or len(indices) % 3:
        raise ValueError(f"Invalid geometry arrays: {record['name']}")
    vertex_count, face_count = len(vertices) // 3, len(indices) // 3
    expected_triangles += face_count
    mesh = bpy.data.meshes.new(record["name"] + " | Mesh")
    mesh.vertices.add(vertex_count)
    mesh.vertices.foreach_set("co", vertices)
    mesh.loops.add(len(indices))
    mesh.loops.foreach_set("vertex_index", indices)
    mesh.polygons.add(face_count)
    mesh.polygons.foreach_set("loop_start", array("i", range(0, len(indices), 3)))
    mesh.polygons.foreach_set("loop_total", array("i", [3]) * face_count)
    mesh.update()
    uv = record.get("uv", [])
    if len(uv) == vertex_count * 2:
        layer = mesh.uv_layers.new(name="UnityUV")
        layer.data.foreach_set("uv", array("f", (uv[vertex * 2 + component]
            for vertex in indices for component in range(2))))
    colors = record.get("colors", [])
    if len(colors) == vertex_count * 4:
        layer = mesh.color_attributes.new(name="UnityColor", type="FLOAT_COLOR", domain="POINT")
        # Contact-shadow values are multiplicative factors, not sRGB paint.
        layer.data.foreach_set("color", colors)
    face_materials = record.get("faceMaterials", [-1] * face_count)
    if len(face_materials) != face_count:
        raise ValueError(f"Material/triangle mismatch: {record['name']}")
    used_ids = list(dict.fromkeys(face_materials))
    slots = {material_id: slot for slot, material_id in enumerate(used_ids)}
    for material_id in used_ids:
        mesh.materials.append(material_map.get(material_id, material_map[-1]))
    mesh.polygons.foreach_set("material_index", array("i", (slots[x] for x in face_materials)))
    normals = record.get("normals", [])
    if len(normals) == len(vertices):
        mesh.polygons.foreach_set("use_smooth", [True] * face_count)
        mesh.normals_split_custom_set_from_vertices(vecs(normals, 3))
    obj = bpy.data.objects.new(record["name"], mesh)
    geometry_collection(record).objects.link(obj)
    obj["Unity path"] = record.get("path", record["name"])
    obj["Unity triangles"] = face_count
    if used_ids and all(x in contact_ids for x in used_ids):
        obj.visible_shadow = False
    if number % 50 == 0:
        print(f"Imported mesh {number + 1}/{len(payload['meshes'])}", flush=True)


for record in payload.get("texts", []):
    text = bpy.data.curves.new(record["name"] + " | Texto", "FONT")
    text.body = record.get("body", "")
    text.size = 1
    text.resolution_u = 8
    text.space_line = 1.05
    text.align_x = ["LEFT", "CENTER", "RIGHT"][max(0, min(2, record.get("alignment", 0)))]
    text.align_y = "BOTTOM_BASELINE"
    obj = bpy.data.objects.new(record["name"], text)
    captions.objects.link(obj)
    obj["Unity path"] = record.get("path", record["name"])
    color = rgba(record.get("color", [1, 1, 1, 1]))
    material = bpy.data.materials.new(record["name"] + " | Tinta")
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.8
    bsdf.inputs["Emission Color"].default_value = color
    bsdf.inputs["Emission Strength"].default_value = 0.45
    text.materials.append(material)
    bpy.context.view_layer.update()
    bounds = [Vector(v) for v in obj.bound_box]
    x_min, x_max = min(v.x for v in bounds), max(v.x for v in bounds)
    y_min, y_max = min(v.y for v in bounds), max(v.y for v in bounds)
    width, height = record.get("width", 0), record.get("height", 0)
    scale_x = width / (x_max - x_min) if width > 0 and x_max > x_min else 0.1
    scale_y = height / (y_max - y_min) if height > 0 and y_max > y_min else scale_x
    anchor = max(0, min(8, record.get("anchor", 4)))
    anchor_x = [x_min, (x_min + x_max) / 2, x_max][anchor % 3]
    anchor_y = [y_max, (y_min + y_max) / 2, y_min][anchor // 3]
    right = Vector(record.get("right", [1, 0, 0])).normalized()
    up = Vector(record.get("up", [0, 0, 1])).normalized()
    normal = right.cross(up).normalized()
    rotation = Matrix((right, up, normal)).transposed().to_4x4()
    position = Vector(record.get("position", [0, 0, 0]))
    # Reconstruct the Unity rectangle anchor from the actual glyph bounds.
    position -= right * anchor_x * scale_x + up * anchor_y * scale_y
    rotation.translation = position
    obj.matrix_world = rotation @ Matrix.Diagonal((scale_x, scale_y, 1, 1))


def aim(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


for record in payload.get("lights", []):
    kind = {"Directional": "SUN", "Point": "POINT", "Spot": "SPOT"}.get(record.get("type"), "POINT")
    lamp = bpy.data.lights.new(record["name"], kind)
    lamp.color = record.get("color", [1, 1, 1])[:3]
    intensity = max(0, record.get("intensity", 1))
    lamp.energy = intensity * (1.0 if kind == "SUN" else 450)
    lamp.use_shadow = False
    if kind == "SUN":
        lamp.angle = math.radians(8)
    else:
        lamp.shadow_soft_size = 0.6 if kind == "POINT" else 0.15
        lamp.use_custom_distance = True
        lamp.cutoff_distance = max(0.1, record.get("range", 10))
    if kind == "SPOT":
        lamp.spot_size = math.radians(record.get("spotAngle", 45))
        lamp.spot_blend = 0.5
    obj = bpy.data.objects.new(record["name"], lamp)
    original_lights.objects.link(obj)
    obj.location = record.get("position", [0, 0, 0])
    direction = Vector(record.get("direction", [0, 0, -1]))
    if direction.length:
        aim(obj, obj.location + direction)
    obj["Unity intensity"] = intensity

# Gentle, editable ceiling fills reproduce Unity's ambient illumination.
for name, position, energy, size in [
    ("Salão | luz suave", (0, 0, 6.0), 1650, 8),
    ("Pintura | luz suave", (0, 17, 4.8), 1000, 7),
    ("Escultura | luz suave", (17, 0, 4.8), 1000, 7),
    ("Literatura | luz suave", (-17, 0, 4.8), 1000, 7),
    ("Entrada | luz suave", (0, -15, 4.8), 650, 5),
]:
    lamp = bpy.data.lights.new(name, "AREA")
    lamp.energy = energy
    lamp.shape = "DISK"
    lamp.size = size
    lamp.color = (1.0, 0.94, 0.84)
    obj = bpy.data.objects.new(name, lamp)
    preview_lights.objects.link(obj)
    obj.location = position
    obj["Purpose"] = "Preview approximation of Unity ambient light"


def camera(name, position, target, degrees=65):
    data = bpy.data.cameras.new(name)
    data.type = "PERSP"
    data.lens_unit = "FOV"
    data.angle = math.radians(degrees)
    data.clip_start, data.clip_end = 0.05, 200
    obj = bpy.data.objects.new(name, data)
    cameras.objects.link(obj)
    obj.location = position
    aim(obj, target)
    data.passepartout_alpha = 0.92
    return obj


hall_camera = camera("01 | Visita ao salão", (-7, -6, 1.7), (3, 4, 3))
gallery_camera = camera("02 | Galeria de pintura", (0, 11.5, 1.7), (0, 19, 2), 65)
camera("03 | Salão e entrada", (7, 6, 1.7), (-3, -4, 2.7), 65)
artworks_camera = camera("04 | Obras da galeria", (0, 16.8, 2), (0.2, 23.8, 2.55), 88)
scene.camera = {"hall": hall_camera, "gallery": gallery_camera, "artworks": artworks_camera}[args.camera]
scene.world = bpy.data.worlds.new("Ambiente | luz de galeria")
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get("Background")
background.inputs["Color"].default_value = (0.56, 0.62, 0.70, 1)
background.inputs["Strength"].default_value = 0.35
scene.render.engine = "CYCLES" if "--cycles" in argv else "CYCLES"
# Set EEVEE using the current identifier (Blender 4.2+ / 5.x).
scene.render.engine = "CYCLES" if args.render and False else "BLENDER_EEVEE"
scene.render.resolution_x, scene.render.resolution_y = 1200, 750
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if hasattr(scene, "eevee"):
    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 32
    if hasattr(scene.eevee, "taa_samples"):
        scene.eevee.taa_samples = 16
scene.view_settings.view_transform = "AgX"
scene.view_settings.exposure = 0.3
scene.view_settings.gamma = 1
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1

# The file opens inside the museum; Material Preview remains responsive.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == "VIEW_3D":
            space = area.spaces.active
            space.clip_start, space.clip_end = 0.05, 200
            space.shading.type = "MATERIAL"
            space.shading.use_scene_world = False
            space.shading.use_scene_lights = False
            space.shading.studiolight_rotate_z = math.radians(35)
            space.shading.studiolight_background_alpha = 0
            space.overlay.show_overlays = False
            space.region_3d.view_perspective = "CAMERA"
            space.region_3d.view_camera_zoom = 28
            space.region_3d.view_location = Vector((3, 4, 3))
            space.region_3d.view_distance = 15
            space.region_3d.view_rotation = scene.camera.rotation_euler.to_quaternion()
        elif area.type == "PROPERTIES":
            area.spaces.active.context = "RENDER"

readme = bpy.data.texts.new("LEIA-ME | Museu atualizado")
readme.write("""MUSEU — SEMANA DE ARTE MODERNA\n\n
Toda a geometria estática exportada da cena Unity está presente, incluindo salas,
tetos, obras, mobiliário e sombras de contato. As imagens estão empacotadas.
As inscrições são objetos de texto editáveis. Cada objeto registra seu caminho Unity.
\nO arquivo abre pela câmera 01 no interior. Há câmeras adicionais na coleção 04.
O modo Material Preview usa iluminação de estúdio para navegação ágil.
F12 usa EEVEE com luzes da cena e preenchimentos de teto editáveis na coleção 03.
\nConversão: Unity (x,y,z) → Blender (x,z,y); escala: 1 unidade = 1 metro.
Materiais, luz ambiente e textos são aproximações visuais, sem lógica de jogo.
""")
actual_triangles = sum(len(obj.data.polygons) for obj in geometry.all_objects if obj.type == "MESH")
if actual_triangles != expected_triangles:
    raise RuntimeError(f"Triangle count mismatch: {actual_triangles} != {expected_triangles}")
report = {
    "source": str(source), "output": str(out), "blender": bpy.app.version_string,
    "meshes": len(payload.get("meshes", [])), "triangles": actual_triangles,
    "texts": len(payload.get("texts", [])), "sourceMaterials": len(payload.get("materials", [])),
    "packedImages": len(image_cache), "sourceLights": len(payload.get("lights", [])),
    "previewLights": len(preview_lights.objects), "warnings": warnings,
}
scene["Import report"] = json.dumps(report, ensure_ascii=False)
bpy.ops.wm.save_as_mainfile(filepath=str(out), check_existing=False)
(out.parent / "import-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2), flush=True)
if args.render:
    scene.render.filepath = str(out.parent / "preview.png")
    bpy.ops.render.render(write_still=True)
