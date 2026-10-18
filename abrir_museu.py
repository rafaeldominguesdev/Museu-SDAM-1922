# Abre o museu com o viewport já em modo Renderizado, pela camera ativa.
import bpy
from bpy.app.handlers import persistent

@persistent
def _preparar(_):
    for w in bpy.context.window_manager.windows:
        for area in w.screen.areas:
            if area.type != 'VIEW_3D':
                continue
            for space in area.spaces:
                if space.type != 'VIEW_3D':
                    continue
                space.shading.type = 'RENDERED'
                space.overlay.show_overlays = True
                space.region_3d.view_perspective = 'CAMERA'
    bpy.app.handlers.load_post.remove(_preparar)

bpy.app.handlers.load_post.append(_preparar)
_preparar(None)
