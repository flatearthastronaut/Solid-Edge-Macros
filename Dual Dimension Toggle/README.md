# Dual Dimension Toggle v1.6

Converts existing non-angular dimensions, Feature Control Frame tolerances, and callout notes on **only the active sheet** of the active Solid Edge draft between inch-only and dual units. Dimension and callout styles match the decimal-place count and `(vert)` suffix; the leading sorting/group number is ignored. Angular dimensions and angle-only callouts retain their current styles in both directions.

## Run

1. Open a draft in Solid Edge and activate the sheet to change.
2. Run `Compiled Executables\Dual Dimension Toggle v1.6.exe`, either directly or through Solid Edge's Run Macro command.
3. Choose **Dual dimensioned to inch only** or **Inch only to dual dimensioned**.
4. Click **Convert active sheet**. Read the conversion results, review the draft, and save it in Solid Edge when ready.

Version 1.2 renames the former Draft Dimension Styles macro to **Dual Dimension Toggle**. Update any existing shortcut or Solid Edge macro button to the new executable path in the `Dual Dimension Toggle` folder.

Versioned executable filenames allow updates to be built while an older macro window is still open. Use **v1.6** for the complete callout conversion, including `/DU` field formatting. Existing shortcuts should point to the v1.6 executable; older executables remain earlier releases.

## Icon

The v1.3 icon shows a white single dimension (1.00 inches), a cyan dual dimension (25.4 [1.00]), and amber switch arrows on a navy background. The executable includes the icon for Explorer, the taskbar, and its window title bar. Separate files are included in `Compiled Executables`:

- `Dual Dimension Toggle.ico`: Windows icon with 16, 24, 32, 48, 64, 128, and 256-pixel sizes.
- `Dual Dimension Toggle.png`: 256-pixel image for macro buttons or shortcuts that accept PNG.

Source artwork is in `Assets/DualDimensionToggle.png`. It was created with the built-in image-generation tool using the prompt saved in `Assets/IconPrompt.txt`. `BuildIcon.ps1` packages the artwork into the icon sizes without regenerating the design. `Build.cmd` runs this local packaging script with a process-only execution-policy setting; no system policy is changed.

The macro attaches to the running Solid Edge instance when you click Convert. It does not start Solid Edge, save/close the draft, or change other sheets. It processes the active sheet's Dimensions, FeatureControlFrames, and callout entries in the Balloons collection, including sheet annotations associated with drawing views. It does not descend into drawing-view internal sheets or change referenced models. All dimensions, frames, and callouts on the sheet are considered, regardless of selection.

## Callout notes

Callouts combine two independent sources of values. The macro handles both:

- **Generated/model-linked values:** change the callout's dimension style using the same decimal-place/orientation matching as ordinary dimensions, and add/remove the `/DU` format option on single-length references. For example, `%{%HS/DU}` becomes `%HS` for inch-only, and `%HS` becomes `%{%HS/DU}` for dual. References stay linked to the model.
- **Explicit metric[inch] text:** remove the metric portion and brackets when switching to inches. Slash-separated limit lists stay together: `6.50/6.35[.256/.250]` becomes `.256/.250`. Multiple pairs and the callout's main, lower, prefix, and suffix fields are supported.
- **Switching back to dual:** restore only pairs previously converted by this macro, using the same one-fewer-decimal-place metric rounding as Feature Control Frames. The macro stores a small conversion record on the annotation, which persists when the draft is saved and reopened. Existing bracketed pairs can also be recalculated from their inch values. Untracked bare decimals such as `.125`, quantities, fractions, and thread designations are never guessed or expanded.

For the example with a linked drill diameter, the literal `6.50/6.35[.256/.250]` diameter range, and a linked depth, the matching style and `/DU` field options control linked lengths while literal conversion handles the range. Surrounding words, line breaks, symbols, quantities, and thread/fraction references are retained.

The counterbore callout converts these references in both directions:

| Inch only | Dual |
| --- | --- |
| `%HS` (hole size) | `%{%HS/DU}` |
| `%BS` (counterbore size) | `%{%BS/DU}` |
| `%BD` (counterbore depth) | `%{%BD/DU}` |

Hole depth `%HD`, countersink size `%SS`, thread depth `%TD`, and bend radius `%BR` are also supported. Symbols such as `%DI`, quantity `%QC`, thread designation `%TS`, smart depth `%ZH`/`%ZT`, complete hole callout `%HC`, angular references, and arbitrary/nested property expressions are left intact. The converter does not infer which arbitrary document properties are lengths.

Existing round-off and tolerance modifiers are retained: `%{%HS/DU/@3/ST+.001^-.002}` becomes `%{%HS/@3/ST+.001^-.002}`. Already wrapped references are not wrapped again. A note already in the target style still gets its field formatting corrected. Pair records saved by v1.5 remain compatible, including when a later inch-only run repairs their `/DU` fields. The record comparison ignores only supported unit-format changes; other edits to tracked text still require review.

Callouts with missing/ambiguous target styles, malformed numeric pairs, or edited text that conflicts with a saved pair record are left unchanged and reported. Ordinary item balloons, parts-list labels, and angle-only callouts are skipped. Custom styles outside the naming convention remain unchanged, while their explicit numeric pairs and supported field formatting may still be converted. Their actual displayed units depend on those custom style definitions.

If text in a tracked field is manually edited after an inch-only conversion, automatic restoration is skipped for that note so the edit is not overwritten. Review that note manually. Styles and raw text are read back after conversion; on failure, the macro attempts to restore the original style, text, and conversion record.

## Feature Control Frame tolerances

The same Convert button also converts the tolerance compartment of each populated frame row, including primary, secondary, tertiary, and quaternary rows:

| Inch only | Dual: millimeters[inches] |
| --- | --- |
| `.001` | `.03[.001]` |
| `.0005` | `.013[.0005]` |
| `.0010` | `.025[.0010]` |
| `.025` | `.64[.025]` |
| `0.001` | `0.03[0.001]` |

- **Dual to inch only:** remove the metric value and brackets, retaining the bracketed inch text exactly. The inch value is authoritative, even if the existing metric value differs from the calculated value.
- **Inch only to dual:** multiply inches by **25.4** and round millimeters to **one fewer decimal place** than the inch text, with a minimum of zero decimal places. Exact halfway cases round away from zero. Preserve the original inch text inside brackets, including trailing zeros. Metric text follows the inch value's leading-zero convention.
- Geometric symbols, diameter/material-condition modifiers, cell boundaries, and datum references (including names such as `A1`) are retained. Frame styles, leaders, and composite settings are not intentionally changed.
- Standalone projected-zone height text is preserved, including when Solid Edge exposes it through the third row. It is not a tolerance value and is not converted.
- Frames already in the requested format are unchanged. Repeating the same direction does not toggle them back.
- If a populated row has an ambiguous or unsupported tolerance, the whole frame is skipped and reported before any of its rows are edited. Supported tolerances are nonnegative decimal values or a metric[inch] pair, optionally surrounded by recognized symbol codes. Fractions, expressions, multiple tolerance values in one cell, comma decimals, and free-text annotations are not guessed.
- Each changed frame is read back and verified. If Solid Edge rejects a change, the macro attempts to restore all original frame rows, reports any restoration failure, and continues with the next frame.

The result separates dimension-style counts from Feature Control Frame counts and reports how many frame rows changed. Review the drawing before saving as usual.

## Style matching

| Inch-only style | Dual style |
| --- | --- |
| `1 2 place` | `2 2 place m[i]` |
| `1 3 place` | `2 3 place m[i]` |
| `1 4 place` | `2 4 place m[i]` |
| `1 5 place` | `2 5 place m[i]` |
| `3 3 place (vert)` | `4 3 place m[i] (vert)` |
| `3 4 place (vert)` | `4 4 place m[i] (vert)` |

Both directions use the same rules. Matching is case-insensitive and tolerates extra spaces. A different grouping number is supported: for example, `20 3 place m[i]` can map to `10 3 place`. The exact target name is read from the draft's own style collection.

- The matching target style must already exist in the draft. Missing matches are skipped and reported.
- If multiple target names have the same decimal count and orientation, the dimension is skipped and reported as ambiguous. The macro does not guess which group to use.
- Angular, arc-angle, and angular-coordinate dimensions are skipped before reading or changing their styles, even if they use a matching style name. The result shows how many were skipped. Arc-length dimensions still convert because they measure length.
- Fraction styles, ANSI/ISO styles, and names outside this naming convention are unchanged.
- Dimensions already in the requested style family are unchanged. Running the same direction twice does not toggle them back.
- Individual errors are reported, and processing continues with the remaining dimensions. If a style assignment fails, the macro attempts to restore the original style name and reports any failure to restore it.

Precision is preserved **by choosing the corresponding N-place named style**, exactly as in the examples. The macro relies on your existing style definitions to have the intended units and precision. Applying a named style uses that style's formatting; it does not preserve arbitrary per-dimension style overrides or copy metric round-off settings into inch settings. Shared style definitions, dimension values, geometry, and the active default dimension style are not edited.

## Build and validation

Requires Windows with 64-bit Solid Edge and .NET Framework 4.x. The executable is standalone; no Siemens interop DLLs need to be distributed. `Build.cmd` uses the Windows .NET Framework C# compiler and writes the executable to `Compiled Executables`.

- `Test.ps1`: compiles the release and runs 505 automated regression assertions covering dimension and frame conversion, rounding, precision, orientation, unsupported input, and failure handling. Callout checks cover both user samples, slash-separated ranges, `/DU` field conversion, unchanged non-length references, formatting modifiers, no inference of bare values, all four text fields, v1.5 record compatibility, stale-record detection, repeated runs, rollback, and unrelated attribute preservation.
- `LiveTest.ps1`: requires a running Solid Edge instance accessible to the test process. Checks both directions on real dimensions, frames, and callouts, unchanged angular annotations, symbols and raw linked text, repeated runs, and an untouched second sheet. Also saves and reopens a separate temporary draft to verify persistent pair restoration. Closes its scratch documents, deletes its temporary draft, and restores the previously active document. Does not modify existing documents.

Validated October 2, 2026: v1.6 release compiled; all 505 regression assertions and 75 live Solid Edge assertions passed. Live checks verify raw linked-field syntax and style assignment on scratch annotations; they do not validate evaluated hole values against a particular production model. Solid Edge ignored suffix assignments on the scratch callout type, so live precision/tolerance checks use the main text; all four fields are covered by regression tests.

## API and lifetime references

The implementation follows the supplied `SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Draft.Sheets/SolidEdge.Draft.Sheets/Program.cs` and `OleMessageFilter.cs` examples: STA automation, connection to the running application, and indexed COM collection traversal. It adds bounded busy-server retries, restores the previous OLE filter, and releases each acquired COM reference in `finally` blocks without forcing shared RCWs to zero.

API signatures and enum values were also verified against the supplied SDK's Framework and FrameworkSupport interop assemblies. The macro selects the named style through the dimension's own `DimStyle.Name`, not the shared `DimensionStyle.Name` in the document catalog. See Siemens' [Dimension.Style](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Dimension~Style.html), [DimStyle.Name](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~DimStyle~Name.html), and [DimStyle members](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~DimStyle_members.html).

Frame collection and row-property signatures were verified against the supplied SDK interop and Siemens' [FeatureControlFrame.PrimaryFrame example](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~FeatureControlFrame~PrimaryFrame.html). Current Solid Edge frame strings use `%VB` cell separators; legacy `VB` and editor `|` separators are also recognized. Live tests established that `ProjectedToleranceFrame` aliases `TertiaryFrame` and can be cleared by a primary-row assignment. The converter reapplies preserved later rows when needed and verifies the final text.

Callout access follows the supplied SDK `SolidEdge.Draft.Balloons/Program.cs` sample: a callout is a Balloon with `Callout = 1`, and its `Style` is a per-object DimStyle. Raw text and Unicode attribute signatures were verified against the SDK interop assemblies. The private `DualDimensionToggleCalloutV1` attribute set holds a versioned record of converted pairs; other attribute sets are preserved. Live save/reopen testing verifies persistence independently of the running macro process.

Single-length reference meanings are documented in Siemens' [Creating detailed drawings, Property text codes, page 9-97](https://support.industrysoftware.automation.siemens.com/training/se/en/ST3/pdf/spse01545.pdf#page=203). `/DU` wrapper conversion follows the user's supplied Solid Edge callout examples; property expressions are parsed as complete balanced tokens to protect unrelated or nested content.
