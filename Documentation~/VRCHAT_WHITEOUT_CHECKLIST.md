# VRChat Whiteout Checklist

Use this checklist before uploading a material that enables strong environment lighting features.

## Required Checks

- Test in a bright VRChat world and confirm the albedo does not wash out to white.
- Compare `Light Volume` ON and OFF. The material should keep roughly the same hue and should not jump in brightness.
- Compare `LTCGI` ON and OFF. Specular, rim, and backlight should stay readable without blooming into flat white.
- Test in a multi-light world with several point or spot lights. Extra lights should brighten the material, but highlights should not stack into a solid white patch.
- Test a dark world after the bright-world check. The material should stay readable without requiring a different preset.

## Recommended Material Baseline

- `_LightColorMax`: `0.9`
- `_GIIntensity`: `0.45`
- `_AdditionalLightIntensity`: `0.35`
- `_SpecularIntensity`: `0.8`

## Failure Conditions

- Base color shifts toward gray or white as soon as a bright light hits it.
- Turning on `Light Volume` causes a sudden brightness jump.
- Turning on `LTCGI` makes specular or rim explode in bright worlds.
- Additional lights repeatedly stack the same highlight pattern.
