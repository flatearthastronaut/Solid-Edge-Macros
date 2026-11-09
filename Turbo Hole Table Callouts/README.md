# Turbo Hole Table Callouts v0.2

Preview and apply native `%F1`–`%F4` selections to recognized hole-table rows in a Solid Edge 2026 draft. Unmatched holes, conflicting matches, existing row overrides, and incompatible format definitions remain unchanged.

## Use

1. Open and activate a saved draft in Solid Edge.
2. Run `Compiled Executables/Turbo Hole Table Callouts v0.2.exe` (the selected table-and-lightning icon A).
3. Click **Scan active draft**. Each native table opens in its own window, labeled with its **Title text**. The main window lists those titles so you can return to any table. Table numbers disambiguate duplicate or blank titles.
4. Review rows in natural hole order (A.2 before A.10). **Already Set** rows are green; **Ready** rows are yellow, including selected cells. Uncheck any proposed changes you do not want, then click **Apply selected formats** in that table's window. Only that table is changed. Successfully applied rows turn green.
5. Review table layout in Solid Edge and save the drawing normally. The macro does not save the original automatically.

The scan evaluates native hole fields in a temporary copy. It does not add diagnostic columns or change callout definitions in the original. Applying a change writes the native `%F` reference, not flattened callout text.

## Recognition

| Category | Format | Evidence |
| --- | --- | --- |
| Standard threaded hole or matching SHCS counterbore | `%F1` | Native thread or matching saved SHCS dimensions |
| Dowel seat | `%F2` | Pin-size chart nominal midpoint and seat depth; matching native fit limits and four-place inch precision |
| #2 SAE port | `%F2` | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |
| Koenig (formerly CV) plug seat | `%F3` | Saved `CV Hole- …mm Plug` seat diameter with unthreaded counterdrill type |
| #4 or #6 SAE port | `%F4` | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |

SAE thread identification is 5/16-24 for #2, 7/16-20 for #4, and 9/16-18 for #6. A thread alone does not establish a port. Matching legacy spotfaces do not require the newer separate countersink/chamfer representation. An older entrance that cannot be identified from the available fields is flagged for review.

The `.052`-inch counterbore is a possible face seal for a 1/16-inch O-ring cross section, not an SAE-port rule. Without a confirming port thread it remains an ambiguous review case. This counterbore check does not reject an identified CV/Koenig counterdrill. Unknown simple holes and other oddballs are also flagged instead of being forced into `%F1`.

The configured Solid Edge **Custom Settings File** supplies the live port, SHCS, and Koenig presets. It is read through application parameter 63. No preset names are used to classify model features; the preset names identify the approved source families, and matching uses native dimensions. **CV/Koenig seat depths and SAE-port counterbore/entrance depths may differ from the saved preset.** Only a positive modeled recess depth is required for those families, not an exact depth match. Passage length and diameter may also vary. Dowel/SHCS depth rules are unchanged. Native callout fields retain the actual model dimensions.

Greg supplied `DraftCallout.txt`, the UTF-16 Solid Edge saved-callout library containing 79 entries. It is retained unchanged as a reference for standard callout text, limits, and notes. Its CV and SAE entries use linked `%BD` depth fields, consistent with variable seat depths. This file describes callout presentation rather than hole-creation geometry; this release continues to read modeled diameters from the active Custom Settings File and evaluates the draft's own `%F1`–`%F4` definitions. It does not replace those definitions or import fixed notes/quantities from the saved-callout library.

`DowelSeats.xml` is a checked transcription of the Dowel Pins table in `C:\Users\greg.lentner\Documents\Codex\checking samples\standards\Pin Sizes.doc` (SHA-256 `7131C32EE4B59439E6C0B6641C0DDF212155A43B5C5271E0D83F86E7C13827C8`). It is embedded in the executable. Update the transcription, source fingerprint, and tests together when the chart changes. Some saved hole-creation presets use a fit endpoint or different depth; those are not silently substituted for the chart midpoint.

## Review cases

- Existing overrides are preserved, including manually entered `%F` selections: the API exposes evaluated cell text, so the macro cannot safely distinguish a manually selected format from custom notes and tolerances.
- A shared slot must contain suitable text for that native hole type. For example, a counterbore `%F4` definition that explicitly says **#4 / 7/16-20** cannot be assigned to a #6 counterbore. The macro flags it rather than rewriting a definition used by other rows.
- Dowel limits must match the chart. The generic ±.00025 format is not sufficient for a chart entry requiring ±.0005.
- This version supports inch-primary `HoleTables2` tables with one `%F` callout column and unique hole labels. Metric-primary, legacy `HoleTables`, ambiguous labels, and multiple callout columns are left unchanged.
- Solid Edge can reorder rows and normalize old cached whitespace during a table refresh. The macro checks hole identities and evaluated results. It does not reposition tables; review spacing and pagination before saving.

## Build and verification

Run `Build.cmd` to compile the standalone x64 .NET Framework executable into `Compiled Executables`. No installed interop DLL needs to be distributed with it.

Run `Test.cmd` for the recognition, format, title, natural sorting, status-color, and table-selection tests. `LiveTest.cmd` uses the active saved draft (which must have at least one eligible change) to create an isolated project copy and checks native titles, scan preservation, rollback, application one table at a time, save/reopen, and retained `%F` associations. It never applies changes to the original drawing. Temporary reports and test copies are under the ignored `Inspection` directory.

v0.2 validation: 38 recognition/template checks and 23 UI behavior checks passed. The active sample had two tables and 38 rows; seven native format assignments passed isolated-table, rollback, save/reopen, and linkage checks on a disposable copy. The original sample stayed unchanged.

For a read-only machine-readable scan:

```text
"Turbo Hole Table Callouts v0.2.exe" --scan "absolute path to an open draft.dft" "absolute report path.xml"
```

COM work stays on the STA thread, releases acquired references, uses the SDK's bounded busy-message filter pattern, and preflights all selected cells before writing. A changed source row or format definition requires another scan. Failed writes attempt to restore the original native column expressions.
