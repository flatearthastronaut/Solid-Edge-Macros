# Dual Dimension Toggle v1.3

Converts existing non-angular dimensions on **only the active sheet** of the active Solid Edge draft between inch-only and dual-dimension named styles. The decimal-place count and `(vert)` suffix are matched; the leading sorting/group number is ignored. Angular dimensions retain their current styles in both directions.

## Run

1. Open a draft in Solid Edge and activate the sheet to change.
2. Run `Compiled Executables\Dual Dimension Toggle.exe`, either directly or through Solid Edge's Run Macro command.
3. Choose **Dual dimensioned to inch only** or **Inch only to dual dimensioned**.
4. Click **Convert active sheet**. Read the conversion results, review the draft, and save it in Solid Edge when ready.

Version 1.2 renames the former Draft Dimension Styles macro to **Dual Dimension Toggle**. Update any existing shortcut or Solid Edge macro button to the new executable path in the `Dual Dimension Toggle` folder.

## Icon

The v1.3 icon shows a white single dimension (1.00 inches), a cyan dual dimension (25.4 [1.00]), and amber switch arrows on a navy background. The executable includes the icon for Explorer, the taskbar, and its window title bar. Separate files are included in `Compiled Executables`:

- `Dual Dimension Toggle.ico`: Windows icon with 16, 24, 32, 48, 64, 128, and 256-pixel sizes.
- `Dual Dimension Toggle.png`: 256-pixel image for macro buttons or shortcuts that accept PNG.

Source artwork is in `Assets/DualDimensionToggle.png`. It was created with the built-in image-generation tool using the prompt saved in `Assets/IconPrompt.txt`. `BuildIcon.ps1` packages the artwork into the icon sizes without regenerating the design. `Build.cmd` runs this local packaging script with a process-only execution-policy setting; no system policy is changed.

The macro attaches to the running Solid Edge instance when you click Convert. It does not start Solid Edge, save/close the draft, or change other sheets. It processes the active sheet's Dimensions collection, including dimensions associated with drawing views. It does not descend into drawing-view internal sheets or change dimensions in referenced models. All dimensions on the sheet are considered, regardless of selection.

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

- `Test.ps1`: compiles the release and runs 114 automated regression assertions covering both directions, decimal count, vertical orientation, grouping numbers, missing/ambiguous styles, unsupported styles, repeated runs, failure recovery, active-sheet scope, unchanged shared style definitions, and all three angular dimension types. Also verifies that an unreadable dimension type prevents changes and that non-angular types still convert.
- `LiveTest.ps1`: requires a running Solid Edge instance accessible to the test process. Creates an unsaved scratch draft, checks both directions on real dimensions, checks inch precision and dual display, verifies angular style names and angular precision remain unchanged, verifies a second sheet remains unchanged, closes the scratch without saving, and restores the previously active document. Does not modify existing documents.

Validated September 30, 2026: v1.3 release compiled; all 114 regression assertions passed. Verified the executable's product name and version, window title, window icon, and executable icon. Conversion behavior is unchanged from v1.1, which passed 34 live Solid Edge assertions, including leaving angular dimensions unchanged.

## API and lifetime references

The implementation follows the supplied `SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Draft.Sheets/SolidEdge.Draft.Sheets/Program.cs` and `OleMessageFilter.cs` examples: STA automation, connection to the running application, and indexed COM collection traversal. It adds bounded busy-server retries, restores the previous OLE filter, and releases each acquired COM reference in `finally` blocks without forcing shared RCWs to zero.

API signatures and enum values were also verified against the supplied SDK's Framework and FrameworkSupport interop assemblies. The macro selects the named style through the dimension's own `DimStyle.Name`, not the shared `DimensionStyle.Name` in the document catalog. See Siemens' [Dimension.Style](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Dimension~Style.html), [DimStyle.Name](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~DimStyle~Name.html), and [DimStyle members](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~DimStyle_members.html).
