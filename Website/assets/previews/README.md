# Feature preview images

Drop feature screenshots here to replace the styled placeholder blocks on the
home page (and, later, doc pages).

## Naming convention

`assets/previews/<slug>.jpg`

The `<slug>` matches the `preview` key of each feature in `Website/home.js`.
Current slugs:

- `face-ortho.jpg`   — Face Ortho Projection
- `ghost.jpg`        — Ghost variant (artifact-free transparency)
- `gpu-particles.jpg`— GPU Particles

## Recommended

- ~800x480 px, JPG or PNG (keep the extension `.jpg` or update the src in home.js)
- Dark background so it blends with the dark theme
- Show the *visual result* of the feature (before/after is ideal)

If a file is missing, `home.js` automatically falls back to the neon placeholder
block, so it is always safe to ship without every image present.
