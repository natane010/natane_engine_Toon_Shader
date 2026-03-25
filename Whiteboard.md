# Whiteboard

## 2026-03-25

### High quality map bake
- Added editor-side high quality bake settings to `MapGenSettings`.
- Implemented supersampled bake, normal-map-aware downsampling, and temporary mesh subdivision for map generation.
- Reused prepared meshes in `GenerateAll` so skinned pose baking and subdivision only happen once per batch.
- Exposed the new bake settings in the MapGenerator inspector, window, and material extension UI.
- Updated the normal auto-generate path so ShaderGUI can enable it from mesh-only scene context as well.
