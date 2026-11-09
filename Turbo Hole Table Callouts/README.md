# Turbo Hole Table Callouts v0.5

Preview and apply native `%F1`–`%F4` selections to recognized hole-table rows in a Solid Edge 2026 draft. Separate native linked expressions handle #6/#8 ports, 3/8-inch spring-pin fits, and dowel fits when shared definitions cannot represent their standards. Unmatched holes, conflicting matches, and existing row overrides remain unchanged.

## Use

1. Open and activate a saved draft in Solid Edge.
2. Run `Compiled Executables/Turbo Hole Table Callouts v0.5.exe` (the selected table-and-lightning icon A).
3. Click **Scan active draft**. Each native table opens in its own window, labeled with its **Title text**. The main window lists those titles so you can return to any table. Table numbers disambiguate duplicate or blank titles.
4. Review rows in natural hole order (A.2 before A.10). **Already Set** rows are green; **Ready** rows are yellow; unmatched/review rows are red, including selected cells. Uncheck any proposed changes you do not want, then click **Apply selected formats** in that table's window. Only that table is changed. Successfully applied rows turn green. The Format column identifies separate expressions as **Linked F2 fit**, **Linked spring pin**, **Linked #6**, or **Linked #8**.
5. Review table layout in Solid Edge and save the drawing normally. The macro does not save the original automatically.

The scan evaluates native hole fields in a temporary copy. It does not add diagnostic columns or change shared callout definitions in the original. Applying a change writes the native `%F` reference or a separate expression containing native hole fields, never flattened callout text.

When a hole-letter group contains multiple types, the entire group receives a continuous four-pixel red outline. That table window displays **Multiple hole types found with the same selected hole size** at the top. The check considers native hole shape, recognized family, fit, and port size within each table. Differences in status or checkbox selection alone do not trigger it. Existing row colors remain visible inside the outline.

## Recognition

| Category | Format | Evidence |
| --- | --- | --- |
| Standard threaded hole or matching SHCS counterbore | `%F1` | Native thread or matching saved SHCS dimensions |
| Dowel pin (slip fit / press fit) | `%F2` or linked F2 fit | Exact saved dowel diameter/depth or chart midpoint/depth; finished chart fit limits and four-place inch precision |
| 3/8-inch spring pin, press fit | Linked spring pin | Exact `.378` diameter / `.62` depth preset; `.382/.375` limits and pin note from `SP 375 PF` |
| #2 SAE port | `%F2` | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |
| Koenig (formerly CV) plug seat | `%F3` | Saved `CV Hole- …mm Plug` seat diameter with unthreaded counterdrill type |
| O-ring face seal | `%F3` | Saved `Face Seal-` counterbore diameter and .052 depth; linked F3 depth limits must evaluate to .054/.050 at three decimal places |
| #4 or #6 SAE port | `%F4` or linked #6 | Saved spotface diameter and native type, or threaded entrance diameter plus matching thread |
| #8 SAE port | `%F4` or linked #8 | Saved `1.181` spotface diameter or `.7616` threaded entrance with `3/4-16`; spotface depth may vary |

SAE thread identification is 5/16-24 for #2, 7/16-20 for #4, 9/16-18 for #6, and 3/4-16 for #8. A thread alone does not establish a port. Matching legacy spotfaces do not require the newer separate countersink/chamfer representation. An older entrance that cannot be identified from the available fields is flagged for review.

The `.052`-inch face-seal recess is for a 1/16-inch O-ring cross section. An unthreaded counterbore must also match one of the saved `Face Seal-` recess diameters (.375, .440, or .500 in the supplied XML). Its inlet drilling may be blind or through. The existing F3 counterbore definition is used only when it retains linked diameters and the saved .054/.050 depth limits. A .052 recess with an unknown diameter stays red for review; a known SAE spotface diameter can still identify a port at .052 depth. Depth alone does not identify either family.

The configured Solid Edge **Custom Settings File** supplies the live dowel, spring-pin, face-seal, port, SHCS, and Koenig presets. It is read through application parameter 63. No preset names are used to classify model features; the preset names identify the approved source families, and matching uses native dimensions. **CV/Koenig seat depths and SAE-port counterbore/entrance depths may differ from the saved preset.** Only a positive modeled recess depth is required for those families, not an exact depth match. Passage length and diameter may also vary. Dowel and supported spring-pin recognition require an exact saved-preset or chart diameter/depth combination. Native callout fields retain the actual model dimensions.

The supplied `Custom.xml` is retained as a regression fixture. Its 110 saved holes match the configured file when supplied; the executable continues reading the configured live file. All 25 saved dowel-pin presets (12 SF/slip fit and 13 PF/press fit) and all three face-seal presets are covered by tests. Categories display **Dowel pin (slip fit)** or **Dowel pin (press fit)** explicitly. Only families listed above have format-selection rules; other saved types remain for review.

Greg supplied `DraftCallout.txt`, the UTF-16 Solid Edge saved-callout library containing 79 entries. It is embedded unchanged. The approved separate #6 fallback reads its `SAE Port #6` entry and uses native hole/pocket depth fields with inch precision. Saved display metadata is excluded. The other shared `%F1`–`%F4` definitions remain unchanged. Rebuild after updating the packaged reference file.

The spring-pin expression preserves the `SP 375 PF` note and replaces its fixed diameter limits with an equivalent model-linked tolerance field. There is no #8 saved-callout entry: its separate expression uses the #8 thread-only preset's `3/4-16` designation and `.563` finite thread extent. It does not substitute the `.689` drilling depth or the spotface preset's unrelated default thread settings. The actual spotface and passage dimensions remain linked, including the sample's changed `.030` spotface depth. Missing or conflicting thread-depth standards remain for review.

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

v0.5 validation: 168 recognition/template checks and 38 UI behavior checks passed. The two-table, 38-row sample has 27 proposed changes and five unmatched rows in red. Top A.1/A.2 are spring pins; front B.1/B.2 are #8 SAE ports with the original .030 spotface depth. Top groups B, D, and E are outlined with the exact warning; the front table has no mixed groups. All 27 changes passed isolated-table, rollback, and save/reopen checks on a disposable copy; all 17 separate expressions retained their model-field links. The original sample stayed unchanged.

For a read-only machine-readable scan:

```text
"Turbo Hole Table Callouts v0.5.exe" --scan "absolute path to an open draft.dft" "absolute report path.xml"
```

COM work stays on the STA thread, releases acquired references, uses the SDK's bounded busy-message filter pattern, and preflights all selected cells before writing. A changed source row or format definition requires another scan. Failed writes attempt to restore the original native column expressions.
