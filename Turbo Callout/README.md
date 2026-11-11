# Turbo Callout v0.5

Fills blank native hole callouts in the active Solid Edge draft. Uses the same recognition source and saved-note library as Turbo Hole Table Callouts, compiled into a standalone x64 .NET Framework executable.

## Use

1. Activate the drawing in Solid Edge.
2. Run `Compiled Executables/Turbo Callout v0.5.exe`.
3. Choose **Inch only** or **Dual — metric[inch]**. Scanning and filling start immediately.
4. Review the result counts and any unchanged callouts. Review the drawing and save it normally.

All working sheets are scanned. Background and internal view sheets are excluded. Ordinary item balloons and parts-list balloons are excluded. All four raw text fields must be empty or whitespace; an existing note is never replaced just because its displayed text is blank. Running again corrects horizontal alignment on existing Turbo Callout notes without changing their text or units. Other existing notes remain unchanged. The macro does not save the drawing.

The supplied Turbo Callout icon is embedded in the executable and shown in the window. The supplied ICO and PNG are also provided beside the executable for macro managers.

## Recognition and notes

`Rules.cs`, `LinkedCallouts.cs`, `DowelSeats.xml`, and `DraftCallout.txt` are compiled directly from the sibling **Turbo Hole Table Callouts** directory. No separately maintained copy of the recognition rules is introduced. At runtime the same configured Solid Edge Custom Settings File (application parameter 63) supplies the hole presets. No Excel installation or external note workbook is required.

The native callout evaluator provides the associated hole type, diameter, depths, recess geometry, and thread data. This matters for multi-feature ports: the parent of a projected edge can be the threaded entrance while the callout's fields evaluate the spotface. A temporary duplicate receives the original leader's exact attachment, a private diagnostic style, and a numeric probe. The probe and style are deleted immediately. The original blank callout is filled in place; it is not replaced or rebound. Diagnostic probing can mark a drawing modified even if no standard matches, but does not save it.

Supported special families follow the shared rules: chart/saved dowel pin seats, 3/8-inch press-fit spring pin, Koenig/CV plug seats, saved face-seal recesses, SAE ports #2/#4/#6/#8, saved SHCS counterbores, and standard threaded holes. After special-family recognition, an otherwise unmatched native simple drilled hole uses the complete saved `HC -DRILL` note for inch-only or `HC -DRILL M[I]` for dual dimensions, including its linked quantity wording. Both blind and through holes retain native Smart Depth. Conflicting special-family matches, inconsistent geometry, unsupported non-simple holes, and unresolved attachments remain blank for review. This fallback is specific to standalone callouts; the hole-table recognition rules are unchanged.

Notes retain native size/depth fields and Smart Depth behavior, actual model dimensions, dowel fit limits and P.F./S.F. lines, face-seal depth limits, and the separate Koenig `(DO NOT CHAMFER HOLE)` line. The saved 12 mm Koenig entry supplies its bracketed inch limits when only a dual entry exists. Threaded holes include the native drilling/minor diameter. Spotface-only SAE features use the size-specific saved thread designation/depth; modeled threaded entrances use their native thread fields. #8 uses the shared rule's saved thread-depth standard. Review the saved standard note if the separate thread feature is later changed.

Inch precision is four places for dowels, two for SHCS counterbores and SAE #6/#8, and three for the other supported families. Dual notes retain the existing metric precision independently. Two metric places are used only if the blank style has no metric unit configured. Tolerance offsets are converted to the primary display units; they remain attached to model fields. Fixed occurrence counts, opposite-side wording, and unresolved `X` placeholders in saved notes are not assumed for an arbitrary leader.

Starting with v0.5, counterbore and threaded notes retain their saved quantity lines. `(%QC)PLC'S EQ. SP. AS SHOWN` remains a separate line in both counterbore unit modes and in dual threaded notes; inch threaded notes retain their saved `TYP(%QC)` wording. `%QC` stays linked for Solid Edge to evaluate. Counterbore notes omit the unresolved `(FOR X S.H.C.S.)` placeholder. These changes apply when filling blank callouts; existing populated notes retain their text.

## Placement and failure handling

A break line extending left (West) produces left-aligned text; a break line extending right (East) produces right-aligned text. This reverses the v0.3 rule per Greg's correction. Rerun the macro to correct notes produced by v0.3 without changing their text, units, or leader geometry. Notes without a horizontal break line keep their original justification. The macro reads the native four-direction `CalloutBalloonBreaklineDirection` property because the older Boolean can be stale in current drafts. The existing callout keeps its leader/text connection, breakline direction and distance, rotation, scale, leader vertices, and free-space point. Fixed text widths are retained; fit-to-content widths grow to accommodate the new text. Native local style setters reject some precision changes, so each filled callout receives a private document style copied from its original style and local presentation. Existing shared styles are not edited. New fills change units, relevant precision, Smart Depth formats, and the requested horizontal alignment. Existing Turbo Callout notes are identified by their private GUID style name; rerun repairs change only their horizontal alignment.

Writes are checked and placement is verified. A failed fill restores the previous text, style name, and placement; a failed restoration is reported explicitly. The program follows the SDK's STA/message-filter pattern and releases each COM reference it acquires. It never calls `FinalReleaseComObject` or starts another Solid Edge session.

## Build and tests

- `Build.cmd`: compile the standalone executable into `Compiled Executables`.
- `Test.cmd`: pure recognition/note and existing-text protection tests.
- `LiveTest.cmd`: create an isolated part/draft fixture under ignored `Inspection`, check rendered inch/dual tolerance limits, failed-fill restoration, other-style preservation, multiple working sheets, and save/reopen links and placement. Returns to the original document.
- `SampleTest.cmd`: make separate inch/dual copies of the active draft under `Inspection`, fill and save/reopen each, verify existing text and all callout placements, and verify original text/placement unchanged. No changes are saved to the original.
- `Inspect.cmd`: inspect native fields of blank callouts; diagnostic probes are removed immediately. `Inspect.cmd --alignment` reads direction and justification without adding probes.
- `Preview.cmd`: render the initial window to `Inspection/window.png`.

v0.5 verified November 11, 2026: 1,243 regression checks passed, including every saved SHCS counterbore preset in both unit modes and blind/through threaded holes. Checks verify the exact saved quantity line occurs once, drilling/thread/Smart Depth fields remain linked, unit mode and precision remain unchanged, and the unresolved screw-size placeholder is omitted. The v0.5 executable compiled successfully. This template-text change was verified by regression tests; live Solid Edge checks below describe earlier releases.

v0.4 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed with the reversed alignment rule. Tests cover both horizontal directions, repair of v0.3 notes without changing text or units, unchanged custom notes, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; alignment and placement survived save/reopen. The original sample's text and placement stayed unchanged.

v0.3 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed. Tests cover both horizontal directions, vertical/no-breakline preservation, alignment-only repairs of previously filled notes, unchanged custom notes and unit styles, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; the requested alignments and existing leader positions survived save/reopen, while the original sample stayed unchanged.

v0.2 verified November 10, 2026: 1,100 regression checks passed, including blind/through plain drills, both saved drill notes, special-family priority, conflicting-match protection, and unchanged shared hole-table rules. The current sample had 14 blank callouts: all 14 filled in each unit mode with zero review cases or apply errors, including the previously unmatched `.4375 × .75` and `.3125 × 1.00` simple holes. Both copies retained callout placement and existing text after save/reopen; repeated runs made no further fills. The original sample's text and placement stayed unchanged. The original v0.1 validation also passed 218 shared recognition/template checks, 68 table UI checks, and 12 isolated Solid Edge checks.

SDK reference: `SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Draft.Balloons` and the SDK COM signatures. Siemens' [Balloon API](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Balloon_members.html) documents the native leader, text, and placement methods used here.
