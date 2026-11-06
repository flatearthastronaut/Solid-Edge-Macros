# Hole Table Formatter v0.1

Preview and apply native `%F1`–`%F4` selections to recognized hole-table rows in a Solid Edge 2026 draft. Unmatched holes, conflicting matches, existing row overrides, and incompatible format definitions remain unchanged.

## Use

1. Open and activate a saved draft in Solid Edge.
2. Run `Compiled Executables/Hole Table Formatter v0.1.exe`.
3. Click **Scan active draft**. Review the current and proposed callouts and the reason for each match.
4. Uncheck any proposed changes you do not want, then click **Apply selected formats**.
5. Review table layout in Solid Edge and save the drawing normally. The macro does not save the original automatically.

The scan evaluates native hole fields in a temporary copy. It does not add diagnostic columns or change callout definitions in the original. Applying a change writes the native `%F` reference, not flattened callout text.

## Recognition

| Category | Format | Evidence |
| --- | --- | --- |
| Standard threaded hole or matching SHCS counterbore | `%F1` | Native thread or matching saved SHCS dimensions |
| Dowel seat | `%F2` | Pin-size chart nominal midpoint and seat depth; matching native fit limits and four-place inch precision |
| #2 SAE port | `%F2` | Saved spotface diameter/depth or matching threaded entrance geometry |
| Koenig plug seat | `%F3` | Saved `CV Hole- …mm Plug` seat diameter/depth |
| #4 or #6 SAE port | `%F4` | Saved spotface diameter/depth or matching threaded entrance geometry |

SAE thread identification is 5/16-24 for #2, 7/16-20 for #4, and 9/16-18 for #6. A thread alone does not establish a port. Matching legacy spotfaces do not require the newer separate countersink/chamfer representation. An older entrance that cannot be identified from the available fields is flagged for review.

The `.052`-inch recess is a possible face seal for a 1/16-inch O-ring cross section, not an SAE-port rule. It remains unchanged. Unknown simple holes and other oddballs are also flagged instead of being forced into `%F1`.

The configured Solid Edge **Custom Settings File** supplies the live port, SHCS, and Koenig presets. It is read through application parameter 63. No preset names are used to classify model features; the preset names identify the approved source families, and matching uses native dimensions. Passage length and passage diameter may vary without changing the plug seat or port spotface identity.

`DowelSeats.xml` is a checked transcription of the Dowel Pins table in `C:\Users\greg.lentner\Documents\Codex\checking samples\standards\Pin Sizes.doc` (SHA-256 `7131C32EE4B59439E6C0B6641C0DDF212155A43B5C5271E0D83F86E7C13827C8`). It is embedded in the executable. Update the transcription, source fingerprint, and tests together when the chart changes. Some saved hole-creation presets use a fit endpoint or different depth; those are not silently substituted for the chart midpoint.

## Review cases

- Existing overrides are preserved, including manually entered `%F` selections: the API exposes evaluated cell text, so the macro cannot safely distinguish a manually selected format from custom notes and tolerances.
- A shared slot must contain suitable text for that native hole type. For example, a counterbore `%F4` definition that explicitly says **#4 / 7/16-20** cannot be assigned to a #6 counterbore. The macro flags it rather than rewriting a definition used by other rows.
- Dowel limits must match the chart. The generic ±.00025 format is not sufficient for a chart entry requiring ±.0005.
- This version supports inch-primary `HoleTables2` tables with one `%F` callout column and unique hole labels. Metric-primary, legacy `HoleTables`, ambiguous labels, and multiple callout columns are left unchanged.
- Solid Edge can reorder rows and normalize old cached whitespace during a table refresh. The macro checks hole identities and evaluated results. It does not reposition tables; review spacing and pagination before saving.

## Build and verification

Run `Build.cmd` to compile the standalone x64 .NET Framework executable into `Compiled Executables`. No installed interop DLL needs to be distributed with it.

Run `Test.cmd` for the recognition and format validation tests. `LiveTest.cmd` uses the supplied open tombstone drawing to create an isolated project copy and checks scan preservation, rollback, native assignments, save/reopen, and retained `%F` associations. It never applies changes to the original drawing. Temporary reports and test copies are under the ignored `Inspection` directory.

For a read-only machine-readable scan:

```text
"Hole Table Formatter v0.1.exe" --scan "absolute path to an open draft.dft" "absolute report path.xml"
```

COM work stays on the STA thread, releases acquired references, uses the SDK's bounded busy-message filter pattern, and preflights all selected cells before writing. A changed source row or format definition requires another scan. Failed writes attempt to restore the original native column expressions.
