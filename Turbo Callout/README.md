# Turbo Callout v0.7

Fills blank native hole callouts in the active Solid Edge draft. Uses the same recognition source and saved-note library as Turbo Hole Table Callouts, compiled into a standalone x64 .NET Framework executable.

## Use

1. Activate the drawing in Solid Edge.
2. Run `Compiled Executables/Turbo Callout v0.7.exe`.
3. Choose **Inch only** or **Dual — metric[inch]**. Scanning and filling start immediately.
4. Review the result counts and any unchanged callouts. Review the drawing and save it normally.

All working sheets are scanned. Background and internal view sheets are excluded. Ordinary item balloons and parts-list balloons are excluded. All four raw text fields must be empty or whitespace for a new fill; an existing note is never replaced just because its displayed text is blank. Running again consolidates previous Turbo Callout styles, adds a missing `TYP(%QC)` line to attached macro notes, and corrects their horizontal alignment. Existing units, precision, tolerances, custom text, and leader geometry are preserved. Other existing notes remain unchanged. The macro does not save the drawing.

The supplied Turbo Callout icon is embedded in the executable and shown in the window. The supplied ICO and PNG are also provided beside the executable for macro managers.

## Recognition and notes

`Rules.cs`, `LinkedCallouts.cs`, `DowelSeats.xml`, and `DraftCallout.txt` are compiled directly from the sibling **Turbo Hole Table Callouts** directory. No separately maintained copy of the recognition rules is introduced. At runtime the same configured Solid Edge Custom Settings File (application parameter 63) supplies the hole presets. No Excel installation or external note workbook is required.

The native callout evaluator provides the associated hole type, diameter, depths, recess geometry, and thread data. This matters for multi-feature ports: the parent of a projected edge can be the threaded entrance while the callout's fields evaluate the spotface. A temporary duplicate receives the original leader's exact attachment, a private diagnostic style, and a numeric probe. The probe and style are deleted immediately. The original blank callout is filled in place; it is not replaced or rebound. Diagnostic probing can mark a drawing modified even if no standard matches, but does not save it.

Supported special families follow the shared rules: chart/saved dowel pin seats, 3/8-inch press-fit spring pin, Koenig/CV plug seats, saved face-seal recesses, SAE ports #2/#4/#6/#8, saved SHCS counterbores, and standard threaded holes. After special-family recognition, an otherwise unmatched native simple drilled hole uses the complete saved `HC -DRILL` note for inch-only or `HC -DRILL M[I]` for dual dimensions, including its linked quantity wording. Both blind and through holes retain native Smart Depth. Conflicting special-family matches, inconsistent geometry, unsupported non-simple holes, and unresolved attachments remain blank for review. This fallback is specific to standalone callouts; the hole-table recognition rules are unchanged.

Notes retain native size/depth fields and Smart Depth behavior, actual model dimensions, dowel fit limits and P.F./S.F. lines, face-seal depth limits, and the separate Koenig `(DO NOT CHAMFER HOLE)` line. The saved 12 mm Koenig entry supplies its bracketed inch limits when only a dual entry exists. Threaded holes include the native drilling/minor diameter. Spotface-only SAE features use the size-specific saved thread designation/depth; modeled threaded entrances use their native thread fields. #8 uses the shared rule's saved thread-depth standard. Review the saved standard note if the separate thread feature is later changed.

Inch precision is four places for dowels, two for SHCS counterbores and SAE #6/#8, and three for the other supported families. Dual notes retain the existing metric precision independently. Two metric places are used only if the blank style has no metric unit configured. Tolerance offsets are converted to the primary display units; they remain attached to model fields. Fixed occurrence counts and opposite-side wording in saved notes are not assumed for an arbitrary leader.

Counterbore and threaded notes retain their saved quantity lines. `(%QC)PLC'S EQ. SP. AS SHOWN` remains a separate line in both counterbore unit modes and in dual threaded notes; inch threaded notes retain their saved `TYP(%QC)` wording. Counterbore notes also retain the exact separate line `(FOR X S.H.C.S.)` after the quantity line, keeping the literal `X` as saved.

Every generated note receives a separate `TYP(%QC)` line if it has no TYP line already. Existing TYP wording is preserved, including fixed quantities. The separate PLC'S line does not suppress TYP. `%QC` stays native so Solid Edge evaluates the associated quantity. Reruns add only missing TYP lines to existing attached Turbo Callout notes; they do not rebuild their earlier saved-note text. TYP detection checks all four callout text fields.

## Placement and failure handling

A break line extending left (West) produces left-aligned text; a break line extending right (East) produces right-aligned text. Notes without a horizontal break line keep their original justification. The macro reads the native four-direction `CalloutBalloonBreaklineDirection` property because the older Boolean can be stale in current drafts. The existing callout keeps its leader/text connection, breakline direction and distance, rotation, scale, leader vertices, and free-space point. Fixed text widths are retained; fit-to-content widths grow to accommodate new text.

Native local style setters reject some precision changes, so formatting uses shared document styles such as `Turbo Callout - Dual 4dp`. Callouts with matching complete style settings reuse the same style. The running Solid Edge type library supplies the readable scalar settings used for comparison; unsupported obsolete properties are excluded consistently, while other read failures stop the change. Different formats remain separate and use a numbered suffix when their unit/precision labels coincide. Existing standard styles are not modified.

Reruns recognize both legacy GUID names and the new shared names. Legacy notes move to matching shared styles with their local overrides verified and restored. Unused legacy styles are removed; Solid Edge's native in-use protection retains styles referenced by other objects or inheritance. Results report how many styles were removed or retained. Temporary styles used for formatting and recognition are removed after use.

Writes are checked and placement is verified. A failed fill restores the previous text, style name, and placement; a failed restoration is reported explicitly. The program follows the SDK's STA/message-filter pattern and releases each COM reference it acquires. It never calls `FinalReleaseComObject` or starts another Solid Edge session.

## Build and tests

- `Build.cmd`: compile the standalone executable into `Compiled Executables`.
- `Test.cmd`: pure recognition/note and existing-text protection tests.
- `LiveTest.cmd`: create an isolated part/draft fixture under ignored `Inspection`; verify rendered limits and TYP quantities, style reuse, legacy cleanup, local override preservation, protected style references, rollback, multiple sheets, and save/reopen. Returns to the original document.
- `SampleTest.cmd [saved-draft-path]`: make separate inch/dual copies of the active draft or supplied sample under `Inspection`, fill/consolidate and save/reopen each, verify allowed TYP additions and all existing formatting/placements, and verify the original unchanged. No changes are saved to the original.
- `Inspect.cmd`: inspect native fields of blank callouts; diagnostic probes are removed immediately. `Inspect.cmd --alignment` reads direction and justification without adding probes.
- `Preview.cmd`: render the initial window to `Inspection/window.png`.

v0.7 verified November 11, 2026: 1,484 regression checks and 26 isolated Solid Edge checks passed. Tests cover TYP lines in every supported hole family, preservation of existing TYP wording, reusable style names, identical-format reuse, rollback without deleting a shared style, legacy consolidation with local overrides, in-use style protection, and save/reopen of formatting and evaluated quantities. In both mode selections, copies of the populated sample consolidated 15 GUID styles into three shared dual styles (2/3/4 inch decimal places), updated all 15 macro notes, and reported zero errors or review cases. Existing note units, formatting, and placements survived save/reopen; repeat runs made no further changes. The source sample stayed unchanged.

v0.6 verified November 11, 2026: 1,273 regression checks passed. Every saved SHCS counterbore preset was checked in both unit modes for exactly one literal `(FOR X S.H.C.S.)` line following the saved quantity line, with native size/depth fields, precision, and units retained. The v0.6 executable compiled successfully. Validation for this text-only change used regression tests.

v0.5 verified November 11, 2026: 1,243 regression checks passed, including every saved SHCS counterbore preset in both unit modes and blind/through threaded holes. Checks verify the exact saved quantity line occurs once, drilling/thread/Smart Depth fields remain linked, unit mode and precision remain unchanged, and the unresolved screw-size placeholder is omitted. The v0.5 executable compiled successfully. This template-text change was verified by regression tests; live Solid Edge checks below describe earlier releases.

v0.4 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed with the reversed alignment rule. Tests cover both horizontal directions, repair of v0.3 notes without changing text or units, unchanged custom notes, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; alignment and placement survived save/reopen. The original sample's text and placement stayed unchanged.

v0.3 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed. Tests cover both horizontal directions, vertical/no-breakline preservation, alignment-only repairs of previously filled notes, unchanged custom notes and unit styles, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; the requested alignments and existing leader positions survived save/reopen, while the original sample stayed unchanged.

v0.2 verified November 10, 2026: 1,100 regression checks passed, including blind/through plain drills, both saved drill notes, special-family priority, conflicting-match protection, and unchanged shared hole-table rules. The current sample had 14 blank callouts: all 14 filled in each unit mode with zero review cases or apply errors, including the previously unmatched `.4375 × .75` and `.3125 × 1.00` simple holes. Both copies retained callout placement and existing text after save/reopen; repeated runs made no further fills. The original sample's text and placement stayed unchanged. The original v0.1 validation also passed 218 shared recognition/template checks, 68 table UI checks, and 12 isolated Solid Edge checks.

SDK reference: `SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Draft.Balloons` and the SDK COM signatures. Siemens' [Balloon API](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Balloon_members.html) documents the native leader, text, and placement methods used here.
