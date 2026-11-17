# Hammill shared build sources

These source files are compiled into Add Sketch, Occurrence Viewer, Batch Approval Export, and the combined Hammill add-in. They are not a separately distributed DLL or installed add-in. O-Ring Finder remains its already-built independent macro.

## Automatic video conversion during packaging

Put recordings in each macro project's `Tooltips` folder, for example `Turbo Callouts/Tooltips/Turbo Callouts.wmv`, then run its normal `Build.cmd`. All 14 maintained distributable tools now package an H.264/yuv420p MP4 beside each WMV: Add Sketch, Occurrence Viewer, Batch Approval Export, O-Ring Finder, Turbo Callouts, Turbo Hole Table Callouts, Bolt Circle Holes, Dual Dimension Toggle, Fill Background Toggle, SketchToModels, Populate SHCS, Resize Base Planes, RightClickConvert, and Update Dimension Styles. Hammill add-in v0.8 already prefers that MP4 for inline tooltips; no add-in update is needed. Source recordings remain unchanged, and an explicitly supplied same-name MP4 takes precedence. Videos are silent in tooltips.

The nine formerly EXE-only builds call `Package-CompiledMacro.ps1` after compilation. This creates a version-matched ZIP in `Compiled Executables`, includes explicitly listed runtime dependencies and companion icons, and excludes unrelated old releases/logs. Existing current-version ZIP copies at a project's root are synchronized to prevent ambiguous Manager publication. Historical projects under `!OLD-OBS`, the SDK, and project-bound detailing/development utilities are not distribution macros.

For future standalone macros, call `Package-CompiledMacro.ps1 -Project . -Executable "Compiled Executables\My Macro v1.0.exe"` after a successful compilation. Supply `-Icon`, `-Image`, and pipe-separated `-DataFiles` for required runtime files as needed. Source `Tooltips` is discovered automatically. Run `Test-AllMacroPackages.ps1` to check current ZIP selection, installation, dependencies, and Turbo ribbon discovery across all 14 tool packages.

`Prepare-VideoTooltips.ps1` converts staged copies, decodes newly converted output to check it, and caches conversions by the WMV's SHA-256 contents and conversion recipe. Replacing a recording invalidates the cache even if its timestamp is unchanged. Conversion failures stop packaging before replacing the previous ZIP. Temporary staging paths are checked before cleanup.

`Get-VideoEncoder.ps1` uses FFmpeg on PATH if available, otherwise downloads the portable Windows essentials build from gyan.dev (linked by ffmpeg.org), verifies the distributor's SHA-256 checksum, and keeps the encoder under `Hammill Common/.tools`. The first WMV conversion requires internet access; subsequent conversions use the local encoder. FFmpeg and its cache are excluded from Git and macro packages. End users need no encoder installation. Files downloaded from the distributor retain their checksum record locally; this is checksum verification, not a digital-signature check.

Run `Test-VideoPackage.ps1` for a real WMV conversion, MP4 codec verification, source preservation, actual Macro Manager installation, launcher discovery, and failed-conversion package preservation. Actual hover playback still requires acceptance in Solid Edge.

- `ComSupport.cs`: deterministic, per-acquisition COM cleanup and the standalone macros' bounded STA message filter, following the bundled SDK automation examples.
- `MacroHost.cs`: attach to the user's running Solid Edge session, validate the active document type, and show a branded standalone window. It never closes the user's documents or Solid Edge.
- `Build-Macro.ps1` and `Build-Icon.ps1`: compile each named x64 macro and create its Hammill-green icon and Macro Manager ZIP in that project's Compiled Executables folder.
- `Test-Macro.ps1`: compile and execute each macro's isolated regression tests.
- `PackageCheck.ps1`: install the four macro ZIPs in a project-local fixture with the actual Macro Manager v1.14; verify stable EXEs/icons and the external O-ring table; confirm the latest Hammill package is one add-in and the v0.7 launcher discovers all four macros. It does not register the add-in or modify a user's installed macros.

Current verification: 235 combined add-in checks, 24 Add Sketch checks, 17 Occurrence Viewer checks, 17 Batch Approval Export checks, and the full package check passed. All three new macro windows were rendered and inspected. Add Sketch v1.2 uses a compact window titled Add Sketch and loaded all four named reference planes in the affected assembly; its live diagnostic made no changes there. Its unchanged sketch engine previously passed native sketch creation in a disposable assembly. The dimension-style command passed 148 live Solid Edge checks; the limit-tolerance command passed 296, including actual rendered limits and Nominal, exact inch/metric precision, and save/reopen. Installed ribbon response and the other new workflows still need acceptance on representative files.
