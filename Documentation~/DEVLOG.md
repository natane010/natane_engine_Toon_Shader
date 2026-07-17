# Whiteboard

## 2026-03-25

### High quality map bake
- Added editor-side high quality bake settings to `MapGenSettings`.
- Implemented supersampled bake, normal-map-aware downsampling, and temporary mesh subdivision for map generation.
- Reused prepared meshes in `GenerateAll` so skinned pose baking and subdivision only happen once per batch.
- Exposed the new bake settings in the MapGenerator inspector, window, and material extension UI.
- Updated the normal auto-generate path so ShaderGUI can enable it from mesh-only scene context as well.

### VCC release prep
- Bumped the package version from `1.5.9` to `1.5.10` for the next VCC/VPM release.
- Synced the README version badge/text and added a `1.5.10` changelog entry.
