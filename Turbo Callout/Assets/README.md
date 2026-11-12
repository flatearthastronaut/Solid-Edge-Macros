# Selected v1.0 application icon

Greg selected option B: white note lines and a bent leader pointing to a hole, with a yellow lightning bolt on a navy rounded-square tile. This matches the theme of Turbo Hole Table Callouts. The selected PNG is preserved as generated; the ICO packages it at 16, 24, 32, 48, 64, 128, and 256 pixels with transparent corners.

Generated with the built-in image-generation tool using `Turbo Hole Table Callouts/Assets/Turbo Hole Table Callouts.png` as the style reference. Shared prompt:

> Use case: logo-brand / style-transfer. Create ONE standalone square Windows application icon concept for Turbo Callout, a CAD macro that fills hole callout notes. The supplied image is a STYLE REFERENCE for its sibling macro Turbo Hole Table Callouts, not the subject to reproduce. Match its exact visual family: dark navy rounded-square tile, very bold clean white pictogram, bright yellow lightning accent, subtly polished flat appearance, consistent corner radius and generous geometric strokes. Tile fills about 94% of the square; genuinely transparent outside rounded corners. Keep silhouette and details legible at 32 pixels. No table grid, no letters, no words, no labels, no watermark, no presentation sheet. The subject must clearly suggest an engineering drawing leader pointing to a hole rather than a generic chat app.

Selected variant prompt:

> Concept B: A compact engineering note represented by three bold white horizontal lines of different lengths across the upper left, without any surrounding box. Below the lines, one bold white bent leader travels right then diagonally down-left to a single outlined circular hole in the lower-left quadrant, with a white filled arrowhead touching the rim. A prominent yellow lightning bolt occupies the lower-right quadrant, matching the reference lightning. Distinct uncluttered shapes, ample negative space. The note lines plus attached leader should be the dominant callout motif.

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Assets/BuildIcon.ps1` from the macro directory to repackage the PNG, then `Build.cmd` to embed it in the v1.0 executable and copy both assets into `Compiled Executables`.
