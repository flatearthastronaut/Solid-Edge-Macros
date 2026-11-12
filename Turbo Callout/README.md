# Turbo Callout v1.0

Fills blank native hole callouts in the active Solid Edge draft. Uses the same recognition source and saved-note library as Turbo Hole Table Callouts, compiled into a standalone x64 .NET Framework executable.

## Use

1. Activate the drawing in Solid Edge.
2. Run `Compiled Executables/Turbo Callout v1.0.exe`.
3. Choose **Inch only** or **Dual — metric[inch]**. Scanning and filling start immediately.
4. Review the result counts and any unchanged callouts. Review the drawing and save it normally.

All working sheets are scanned. Background and internal view sheets are excluded. Ordinary item balloons and parts-list balloons are excluded. All four raw text fields must be empty or whitespace for a new fill; an existing note is never replaced just because its displayed text is blank. Running again applies the precision, stacked-text height, and quantity-line rules below to existing attached Turbo Callout notes, consolidates their styles, and corrects their horizontal alignment. Existing unit mode, manufacturing limits, other wording/formatting, and leader geometry are preserved. Other existing notes remain unchanged. The macro does not save the drawing.

The supplied Turbo Callout icon is embedded in the executable and shown in the window. The supplied ICO and PNG are also provided beside the executable for macro managers.

## Recognition and notes

`Rules.cs`, `LinkedCallouts.cs`, `DowelSeats.xml`, and `DraftCallout.txt` are compiled directly from the sibling **Turbo Hole Table Callouts** directory. No separately maintained copy of the recognition rules is introduced. At runtime the same configured Solid Edge Custom Settings File (application parameter 63) supplies the hole presets. No Excel installation or external note workbook is required.

The native callout evaluator provides the associated hole type, diameter, depths, recess geometry, and thread data. This matters for multi-feature ports: the parent of a projected edge can be the threaded entrance while the callout's fields evaluate the spotface. A temporary duplicate receives the original leader's exact attachment, a private diagnostic style, and a numeric probe. The probe and style are deleted immediately. The original blank callout is filled in place; it is not replaced or rebound. Diagnostic probing can mark a drawing modified even if no standard matches, but does not save it.

Supported special families follow the shared rules: chart/saved dowel pin seats, 3/8-inch press-fit spring pin, Koenig/CV plug seats, saved face-seal recesses, SAE ports #2/#4/#6/#8, saved SHCS counterbores, and standard threaded holes. After special-family recognition, an otherwise unmatched native simple drilled hole uses the complete saved `HC -DRILL` note for inch-only or `HC -DRILL M[I]` for dual dimensions, including its linked quantity wording. Both blind and through holes retain native Smart Depth. Conflicting special-family matches, inconsistent geometry, unsupported non-simple holes, and unresolved attachments remain blank for review. This fallback is specific to standalone callouts; the hole-table recognition rules are unchanged.

Notes retain native drilling/depth fields and Smart Depth behavior, dowel fit limits and P.F./S.F. lines, face-seal depth limits, and the separate Koenig `(DO NOT CHAMFER HOLE)` line. The fixed higher-precision dual values are described below. The saved 12 mm Koenig entry supplies its bracketed inch limits when only a dual entry exists. Threaded holes include the native drilling/minor diameter. Spotface-only SAE features use the size-specific saved thread designation/depth; modeled threaded entrances use their native thread fields. #8 uses the shared rule's saved thread-depth standard. Review the saved standard note if the separate thread feature is later changed.

Precision is assigned to each machining value:

| Value | Inch places | Metric places |
| --- | ---: | ---: |
| Drill diameters and depths | 2 | 1 |
| Counterbore/spotface diameters and ordinary depths | 2 | 1 |
| Dowel-pin diameters | 4 | 3 |
| Spring-pin diameters | 3 | 2 |
| Dowel-pin, spring-pin, and thread depths | 2 | 1 |
| Koenig seat limit diameters | 3 | 2 |
| O-ring face-seal depth limits (.054/.050 inch) | 3 | 2 |

Every note remains a **single native callout**. Inch-only notes use native per-field round-off and retain linked sizes and limits. Solid Edge's dual formatter has one shared inch precision for the whole callout, so dual notes follow Greg's saved-note fallback: higher-precision diameters and face-seal depth limits are **fixed numeric text in both units**, with stacked limits. Drill diameters, counterbore/spotface diameters, ordinary depths, Smart Depth, and quantities remain native and use two-place inch/one-place metric formatting. Fixed values are not regenerated on reruns; review them if the model changes. No grouped callouts or hidden helper annotations are added. Model geometry is never changed.

For existing macro notes, native machining fields, precision settings, stacked-text height, and the quantity-line rules below are updated. Existing literal dimensions and other custom wording stay intact; unsupported custom tolerance formatting or unit order is left for review. Fixed occurrence counts and opposite-side wording in saved notes are not assumed for an arbitrary leader.

Standard threaded holes and simple SHCS counterbores use exactly one `(%QC)PLC'S EQ. SP. AS SHOWN` quantity line, with **no TYP line**, in both inch-only and dual modes. Reruns replace the old TYP wording or remove the redundant TYP line across all four native text fields. An existing spacing line keeps its position; custom TYP lines containing additional manufacturing wording are left for review. Counterbore notes retain the exact separate line `(FOR X S.H.C.S.)`, including when a single occurrence makes the quantity line unnecessary. Special families such as SAE ports, face seals, and pin holes retain their own quantity policy.

When the native `%QC` quantity is one, omit the **entire TYP and associated PLC'S EQ. SP. quantity lines**, including a combined TYP/spacing line. Literal `TYP(1)` lines are omitted too. No blank line is left behind. For families other than standard threads and SHCS counterbores, add `TYP(%QC)` if no TYP line exists and the quantity is not one, retain the saved spacing line above one, and preserve other fixed TYP quantities. Retained `%QC` fields stay model-linked. An unresolved quantity is not assumed to be one. This applies to new fills and all four text fields of existing Turbo Callout notes. Rerun the macro after a quantity change: standard threads and SHCS counterbores regain the spacing line when their quantity increases, while other families regain TYP. Other saved-note wording is not rebuilt.

Typed stacked tolerance values use **100% of the surrounding note height**, giving .125-inch characters in the standard .125-inch notes. Solid Edge applies `ToleranceHeightRatio` to literal `/ST` stacks; its usual 80% setting made them .100 inch. New fills and reruns correct that ratio only for macro notes containing typed numeric stacks. Fixed values, precision, base text height, and leader placement are preserved. Notes without typed stacks retain their existing tolerance-height setting, and matching formats continue to share styles.

Machining text uses two spaces before and after `DRILL`, two spaces after `THR'D`, `SPOTFACE`, and `C'BORE`/`CBORE`, and three spaces after `DIA` (one extra around `DRILL` and after the thread/spotface/counterbore labels, two extra after `DIA`). New fills and existing macro notes receive the spacing in all four text fields. Reruns do not accumulate more spaces; wider custom gaps, label spelling, and native property/stack codes are preserved. Values, precision, text height, and leader geometry are unchanged.

## Placement and failure handling

A break line extending left (West) produces left-aligned text; a break line extending right (East) produces right-aligned text. Notes without a horizontal break line keep their original justification. The macro reads the native four-direction `CalloutBalloonBreaklineDirection` property because the older Boolean can be stale in current drafts. The existing callout keeps its leader/text connection, breakline direction and distance, rotation, scale, leader vertices, and free-space point. Fixed text widths are retained; fit-to-content widths grow to accommodate new text.

Native local style setters reject some precision changes, so formatting uses shared document styles such as `Turbo Callout - Dual 2dp`. Per-field overrides or fixed limits provide the additional precision without extra styles. Callouts with matching complete base settings reuse the same style; existing local overrides are restored onto the individual note. The running Solid Edge type library supplies the readable scalar settings used for comparison; unsupported obsolete properties are excluded consistently, while other read failures stop the change. Different base formats remain separate and use a numbered suffix when their labels coincide. Existing standard styles are not modified.

Reruns recognize both legacy GUID names and the new shared names. Existing notes move to matching shared styles with their local overrides verified and restored. Unused macro styles, including older shared precision styles, are removed; Solid Edge's native in-use protection retains styles referenced by other objects or inheritance. Results report how many styles were removed or retained. Temporary styles used for formatting and recognition are removed after use.

Writes are checked and placement is verified. A failed fill restores the previous text, style name, and placement; a failed restoration is reported explicitly. The program follows the SDK's STA/message-filter pattern and releases each COM reference it acquires. It never calls `FinalReleaseComObject` or starts another Solid Edge session.

## Build and tests

- `Build.cmd`: compile the standalone executable into `Compiled Executables`.
- `Test.cmd`: pure recognition/note and existing-text protection tests.
- `LiveTest.cmd`: create an isolated part/draft fixture under ignored `Inspection`; verify rendered mixed precision, fixed versus parametric model-update behavior, TYP quantities, one-callout preservation, style reuse, legacy cleanup, local override preservation, protected styles, rollback, multiple sheets, and save/reopen. Returns to the original document if one was active.
- `SampleTest.cmd [saved-draft-path] [--blank]`: make separate inch/dual copies of the active draft or supplied sample under `Inspection`, fill/update and save/reopen each, verify requested precision changes and all other formatting/placements, and verify the original unchanged. `--blank` empties only macro notes in the disposable copies to test initial filling against the sample's real model associations. No changes are saved to the original.
- `Inspect.cmd`: inspect native fields of blank callouts; diagnostic probes are removed immediately. `Inspect.cmd --alignment` reads direction and justification without adding probes.
- `Preview.cmd`: render the initial window to `Inspection/window.png`.

v1.0 released November 12, 2026: 5,162 regression checks passed. The final spacing rule adds one extra space after C'BORE/CBORE, preserving spelling, wider custom gaps, and stable reruns. Populated sample copies updated only the counterbore note, with zero errors or review cases. Save/reopen and repeat-run checks passed; values, precision, text height, styles, model links, and leader placement were retained. The source sample remained unchanged. Executable and assembly versions are 1.0 and 1.0.0.0 respectively.

v0.13 verified November 12, 2026: 5,060 regression checks passed, including THR'D/SPOTFACE spacing, adjacent words, preserved field codes, and stable reruns. Sample copies updated only the two standard threaded notes and three SAE-port notes, with zero errors or review cases. Save/reopen and repeat-run checks passed; existing values, precision, .125-inch stack height, styles, model links, and leader placement were retained. The source sample remained unchanged.

v0.12 verified November 12, 2026: 4,940 regression checks and 35 isolated Solid Edge checks passed. All supported templates have the requested DRILL/DIA spacing in both unit modes, including stable reruns, adjacent keywords, and protected field/stack codes. Populated sample copies updated all 15 notes with zero errors or review cases, retained their styles, heights, precision, model links, and leader positions, and passed save/reopen and repeat-run checks. The source sample remained unchanged.

v0.11 verified November 12, 2026: 4,443 regression checks and 35 isolated Solid Edge checks passed. The native rendering check confirmed that changing the stack ratio from 80% to 100% increases the rendered stack by 25%, with .125-inch base height and identical typed text. Copies of the populated sample updated only its seven notes containing typed stacks, with zero errors or review cases. Values, precision, leader placement, model attachments, save/reopen, repeat-run stability, and the untouched source sample were verified. Other note formats remained unchanged.

v0.10 verified November 12, 2026: 4,250 regression checks passed. Copies of the populated sample updated only its two standard threaded notes and one SHCS counterbore, with zero errors or review cases. Initial-fill copies filled all 15 leaders in each unit mode. Threads and simple counterbores show exactly one linked spacing line and no TYP; the counterbore purpose line remains. Save/reopen, repeated runs, unchanged precision/style settings, leader placement, model attachments, and the untouched source sample were verified.

v0.9 verified November 12, 2026: 3,642 regression checks and 33 isolated Solid Edge checks passed. New inch and dual notes omit TYP at native quantity one. Existing notes remove the complete TYP(1) and associated spacing lines, retain other fixed TYP quantities and manufacturing text, and preserve precision, leaders, styles, and model attachments through save/reopen. Repeated runs do not reinsert the omitted lines.

v0.8 verified November 11, 2026: 3,158 regression checks and 31 isolated Solid Edge checks passed. Native rendering confirms four-place inch/three-place metric fixed dowel limits beside a two-place inch/one-place metric depth in one callout. Changing the fixture model updates the native depths and inch-only diameter while leaving the dual fixed limits unchanged. The populated sample updated all 15 existing notes without errors, removed its three obsolete shared styles, and retained one shared dual style. Initial-fill copies filled all 15 leaders in each unit mode, including simple drills, pin holes, threads, SAE ports, counterbores, Koenig seats, and a face seal. Placement, model attachments, quantity wording, formatting outside the precision changes, save/reopen, and repeat-run stability were checked. No changes were saved to the source sample.

v0.7 verified November 11, 2026: 1,484 regression checks and 26 isolated Solid Edge checks passed. Tests cover TYP lines in every supported hole family, preservation of existing TYP wording, reusable style names, identical-format reuse, rollback without deleting a shared style, legacy consolidation with local overrides, in-use style protection, and save/reopen of formatting and evaluated quantities. In both mode selections, copies of the populated sample consolidated 15 GUID styles into three shared dual styles (2/3/4 inch decimal places), updated all 15 macro notes, and reported zero errors or review cases. Existing note units, formatting, and placements survived save/reopen; repeat runs made no further changes. The source sample stayed unchanged.

v0.6 verified November 11, 2026: 1,273 regression checks passed. Every saved SHCS counterbore preset was checked in both unit modes for exactly one literal `(FOR X S.H.C.S.)` line following the saved quantity line, with native size/depth fields, precision, and units retained. The v0.6 executable compiled successfully. Validation for this text-only change used regression tests.

v0.5 verified November 11, 2026: 1,243 regression checks passed, including every saved SHCS counterbore preset in both unit modes and blind/through threaded holes. Checks verify the exact saved quantity line occurs once, drilling/thread/Smart Depth fields remain linked, unit mode and precision remain unchanged, and the unresolved screw-size placeholder is omitted. The v0.5 executable compiled successfully. This template-text change was verified by regression tests; live Solid Edge checks below describe earlier releases.

v0.4 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed with the reversed alignment rule. Tests cover both horizontal directions, repair of v0.3 notes without changing text or units, unchanged custom notes, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; alignment and placement survived save/reopen. The original sample's text and placement stayed unchanged.

v0.3 verified November 10, 2026: 1,107 regression checks and 17 isolated Solid Edge checks passed. Tests cover both horizontal directions, vertical/no-breakline preservation, alignment-only repairs of previously filled notes, unchanged custom notes and unit styles, rollback, and saved leader positions/model links. Both inch and dual copies of the current sample filled all 15 blank notes with zero review cases or errors; the requested alignments and existing leader positions survived save/reopen, while the original sample stayed unchanged.

v0.2 verified November 10, 2026: 1,100 regression checks passed, including blind/through plain drills, both saved drill notes, special-family priority, conflicting-match protection, and unchanged shared hole-table rules. The current sample had 14 blank callouts: all 14 filled in each unit mode with zero review cases or apply errors, including the previously unmatched `.4375 × .75` and `.3125 × 1.00` simple holes. Both copies retained callout placement and existing text after save/reopen; repeated runs made no further fills. The original sample's text and placement stayed unchanged. The original v0.1 validation also passed 218 shared recognition/template checks, 68 table UI checks, and 12 isolated Solid Edge checks.

SDK reference: `SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Draft.Balloons` and the SDK COM signatures. Siemens' [Balloon API](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Balloon_members.html) documents the native leader, text, and placement methods used here.
