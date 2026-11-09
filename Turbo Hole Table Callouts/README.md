# Turbo Hole Table Callouts v0.3

Preview and apply native `%F1`–`%F4` selections to recognized hole-table rows in a Solid Edge 2026 draft. Separate native linked expressions handle #6 ports when shared F4 describes #4, and dowel fits when the modeled preset nominal differs from the chart midpoint. Unmatched holes, conflicting matches, and existing row overrides remain unchanged.

## Use

1. Open and activate a saved draft in Solid Edge.
2. Run `Compiled Executables/Turbo Hole Table Callouts v0.3.exe` (the selected table-and-lightning icon A).
3. Click **Scan active draft**. Each native table opens in its own window, labeled with its **Title text**. The main window lists those titles so you can return to any table. Table numbers disambiguate duplicate or blank titles.
4. Review rows in natural hole order (A.2 before A.10). **Already Set** rows are green; **Ready** rows are yellow; unmatched/review rows are red, including selected cells. Uncheck any proposed changes you do not want, then click **Apply selected formats** in that table's window. Only that table is changed. Successfully applied rows turn green. The Format column identifies separate expressions as **Linked F2 fit** or **Linked #6**.
5. Review table layout in Solid Edge and save the drawing normally. The macro does not save the original automatically.

The scan evaluates native hole fields in a temporary copy. It does not add diagnostic columns or change shared callout definitions in the original. Applying a change writes the native `%F` reference or a separate expression containing native hole fields, never flattened callout text.

## Recognition

| Category | Format | Evidence |
| --- | --- | --- |
| Standard threaded hole or matching SHCS counterbore | `%F1` | Native thread or matching saved SHCS dimensions |
| Dowel seat | `%F2` or linked F2 fit | Exact saved dowel diameter/depth or chart midpoint/depth; finished chart fit limits and four-place inch precision |
| #2 SAE port | `%F2` | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |
| Koenig (formerly CV) plug seat | `%F3` | Saved `CV Hole- …mm Plug` seat diameter with unthreaded counterdrill type |
| #4 or #6 SAE port | `%F4` or linked #6 | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |

SAE thread identification is 5/16-24 for #2, 7/16-20 for #4, and 9/16-18 for #6. A thread alone does not establish a port. Matching legacy spotfaces do not require the newer separate countersink/chamfer representation. An older entrance that cannot be identified from the available fields is flagged for review.

The `.052`-inch counterbore is a possible face seal for a 1/16-inch O-ring cross section, not an SAE-port rule. Without a confirming port thread it remains an ambiguous review case. This counterbore check does not reject an identified CV/Koenig counterdrill. Unknown simple holes and other oddballs are also flagged instead of being forced into `%F1`.

The configured Solid Edge **Custom Settings File** supplies the live dowel, port, SHCS, and Koenig presets. It is read through application parameter 63. No preset names are used to classify model features; the preset names identify the approved source families, and matching uses native dimensions. **CV/Koenig seat depths and SAE-port counterbore/entrance depths may differ from the saved preset.** Only a positive modeled recess depth is required for those families, not an exact depth match. Passage length and diameter may also vary. Dowel recognition requires an exact saved-preset or chart diameter/depth combination. Native callout fields retain the actual model dimensions.

Greg supplied `DraftCallout.txt`, the UTF-16 Solid Edge saved-callout library containing 79 entries. It is embedded unchanged. The approved separate #6 fallback reads its `SAE Port #6` entry and uses native hole/pocket depth fields with inch precision. Saved display metadata is excluded. The other shared `%F1`–`%F4` definitions remain unchanged. Rebuild after updating the packaged reference file.

`DowelSeats.xml` is a checked transcription of the Dowel Pins table in `C:\Users\greg.lentner\Documents\Codex\checking samples\standards\Pin Sizes.doc` (SHA-256 `7131C32EE4B59439E6C0B6641C0DDF212155A43B5C5271E0D83F86E7C13827C8`). It is embedded in the executable. Update the transcription, source fingerprint, and tests together when the chart changes. Exact saved modeling sizes are recognized separately from the chart midpoint. For example, the 10mm PF preset models .3932, while the finished fit is .3937/.3932. A separate linked expression uses the required asymmetric tolerance offsets, preserving the model size, finished limits, and the table's shared F2 definition. The same approach handles presets modeled as drill stock.

## Review cases

- Existing overrides are preserved, including manually entered `%F` selections: the API exposes evaluated cell text, so the macro cannot safely distinguish a manually selected format from custom notes and tolerances.
- A shared slot must contain suitable text for that native hole type. A counterbore `%F4` definition that explicitly says **#4 / 7/16-20** cannot label a #6 counterbore; **Linked #6** handles this case without changing #4 rows. Other unsupported template conflicts remain red for review.
- Dowel limits must match the chart. A generic ±.00025 around a fit endpoint or drill-stock diameter produces the wrong finished limits. **Linked F2 fit** adjusts native tolerance offsets and retains four decimal places.
- This version supports inch-primary `HoleTables2` tables with one `%F` callout column and unique hole labels. Metric-primary, legacy `HoleTables`, ambiguous labels, and multiple callout columns are left unchanged.
- Solid Edge can reorder rows and normalize old cached whitespace during a table refresh. The macro checks hole identities and evaluated results. It does not reposition tables; review spacing and pagination before saving.
- Separate row expressions link numeric dimensions to their holes. If a hole later changes between blind and through, review/update its separate row text: the THRU wording is selected during the scan. Newly applied rows are preserved as existing overrides on later scans, like manually assigned formats; a rescan does not replace them automatically.

## Build and verification

Run `Build.cmd` to compile the standalone x64 .NET Framework executable into `Compiled Executables`. No installed interop DLL needs to be distributed with it.

Run `Test.cmd` for the recognition, finished fits, saved-callout parsing, title, sorting, status-color, and table-selection tests. `LiveTest.cmd` uses the active saved draft (which must have at least one eligible change) to create an isolated project copy and checks native titles, scan preservation, rollback, application one table at a time, save/reopen, retained `%F` associations, and reevaluation of separate linked expressions. It never applies changes to the original drawing. Temporary reports and test copies are under the ignored `Inspection` directory.

v0.3 validation: 53 recognition/template checks and 27 UI behavior checks passed. All 12 previously missed dowels and the #6 port are Ready in the sample. The two-table, 38-row sample has 20 proposed changes total and 12 unmatched rows in red. All 20 changes passed isolated-table, rollback, and save/reopen checks on a disposable copy; all 13 separate expressions retained their model-field links. The original sample stayed unchanged.

For a read-only machine-readable scan:

```text
"Turbo Hole Table Callouts v0.3.exe" --scan "absolute path to an open draft.dft" "absolute report path.xml"
```

COM work stays on the STA thread, releases acquired references, uses the SDK's bounded busy-message filter pattern, and preflights all selected cells before writing. A changed source row or format definition requires another scan. Failed writes attempt to restore the original native column expressions.
