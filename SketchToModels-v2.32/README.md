# Sketch to Models v2.35 — close completed parts

Closes each successfully completed part after saving it and the assembly, before continuing to the next row. The optional graphical sketch-selection feature is retained. Standard, Bushing Style and O-ring workflows, color rules, the embedded icon, and the end-of-batch COM cleanup are retained. The maintained source folder is still named SketchToModels-v2.32; older versioned executables remain available.

## Run

Close older macro instances. With a saved assembly active in Solid Edge 2026, run `Compiled Executables/SketchToModels-v2.35.exe`. Update the ribbon button to this executable if it points at an earlier version. Its matching PNG is included.

The assembly filename must contain exactly one isolated five-digit drawing number. Output remains `<sketch name> <number>.par` beside the assembly. Existing files are blocked, never overwritten.

## Closing completed parts

Standard, Bushing Style and O-ring parts all close automatically after their modeling, validation, coloring and final saves succeed. The assembly stays open and its component occurrences remain in place. Only the newly generated part document is closed; other open documents are not closed. Failed or canceled work stays open for inspection. If closing itself fails, the batch stops and reports that stage. The close operation uses SaveChanges=true and runs only on the success path, never in error cleanup.

## Optional automatic selection

**Try automatic sketch selection for revolves** is unchecked at each launch. With it off, use the established manual Project to Sketch / Wireframe Chain workflow. With it on, v2.35 waits until Wireframe Chain is selected, computes a point on a non-construction curve of the newly copied sketch, and sends one graphical click to Solid Edge's drawing window. This replaces v2.33's ineffective SelectSet preselection.

The instruction popup briefly hides to uncover the drawing. Model coordinates are converted using the current view; the target window, owning process, visible bounds and active command are checked before clicking. Physical-pixel coordinates are used on scaled monitors. Covered/offscreen curves and the predefined Z axis are avoided. There are no repeated click attempts after an input failure. Keep the outline visible and allow the short automatic pick to finish before moving the pointer.

**Check the highlighted chain and accept it yourself**, then Close Sketch, Finish the 360-degree feature, and Continue. If nothing is selected, or you want a different chain, click **Select manually** and pick the outline. This picks a chain from an outline edge; it does not automatically choose every disconnected loop in a complex sketch. The existing final validation is retained. A sent click is not reported as a completed or verified projection.

Bushing Style uses the same graphical pick in both the surface and solid profile. You still pick the copied sketch for the surface's Feature's Plane step. Its Z axis remains automatic; the solid's axis remains user-selected. O-ring parts ignore this option.

All twelve regression programs passed, including save-before-close ordering, preserving open parts on modeling/cancellation/save errors, stopping on a close error, and both prompt handoffs and native mouse-message delivery to a test panel, process/bounds/visibility guards, manual fallback and default-off behavior. Full live Solid Edge graphical selection still requires an operator test. A direct API projection alternative was tested in disposable CAD parts and rejected because the projected outline failed the source-update check; it is not used by this release.

## O-ring

1. Check **O-ring** beside each desired assembly sketch. This alone selects the row; Select is optional. Choosing O-ring clears Bushing Style and vice versa.
2. The template defaults to **O-ring Template.par** in the same folder as the selected normal.par. If it is elsewhere, choose it using the O-ring template Browse button. Leaving that field blank restores the default. A successful batch saves the override beside the executable.
3. Click **Process**. The macro creates the part at default placement and copies the assembly sketch with its link, as before.
4. It moves the existing ordered feature **Protrusion 6** immediately after the newly copied sketch, verifies the order and feature status, then saves the part and assembly and advances to the next selected row. There is no new revolve command or modeling popup for O-ring parts.

The O-ring template must contain exactly one ordered feature named Protrusion 6 (internal name or PathFinder name). The macro preserves its profile and parameters; reordering does not replace its profile with the copied sketch. A missing/ambiguous feature, failed reorder, invalid feature status, or missing sketch link stops processing with a diagnostic. Partial work is preserved for inspection.

Only templates used by selected rows are required. An O-ring-only batch can use an explicit O-ring template without an accessible normal.par. The CAD template itself is not bundled in this release.

## Existing workflows and colors

- **Select:** normal.par, default placement, linked sketch, Right (yz) solid revolve, automatic part-Z axis, Project to Sketch and Wireframe Chain.
- **Bushing Style:** normal.par, graphical coordinate-system placement, linked sketch, Feature's Plane revolved surface with automatic local-Z centerline, then Right (yz) solid revolve with user-chosen axis. Finish each feature and click Continue.
- Existing styles only: collet/diaphragm Yellow; part stop/partstop Green; adapter Khaki; puller Orange; body Bronze; insert/inserts Blue. Missing styles are skipped; no styles are created.
- **O-ring overrides every name rule:** parts selected with O-ring use the existing **Black** style, including the template's Protrusion 6 feature. If Black is unavailable, coloring is skipped without falling back to a name-based color.

## Source, build and validation

C#/.NET Framework, x64 Windows Forms. Run `Build.cmd` on Windows with Solid Edge 2026 installed. It compiles `SketchToModels.cs`, `ComSupport.cs`, `ORingWorkflow.cs`, and `AutoSketchSelection.cs`; runnable files go in **Compiled Executables**. `SketchToModels-Commented.cs` mirrors the documented main source; do not compile both together.

Run `Test.ps1` for twelve regression programs. Tests additionally check the default-off option, exact source selection after Wireframe Chain, single-attempt behavior, rejected selection/click, manual handoff and both prompt paths. Tests cover template selection/preflight, selection exclusivity, the O-ring create/copy/reopen/reorder/save path, existing template sketches, missing/duplicate features, failed/no-op reorders, feature errors, COM/thread guards, existing modeling workflows, colors, and native dropdown selection. All twelve passed for this release. Tests use simulated CAD objects and real Windows controls; they do not validate live Solid Edge geometry or the user's actual template. Test one O-ring part before processing a batch.

The implementation references the repository SDK Automation sample and its interop signatures. Feature ordering uses Siemens' [Reorder(target, InsertBefore)](https://support.industrysoftware.automation.siemens.com/trainings/se/106/api/SolidEdgePart~ExtrudedProtrusion~Reorder.html), with false to insert after, and reads back [DesignEdgebarFeatures](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~PartDocument~DesignEdgebarFeatures.html). Temporary wrappers remain scoped to the operation and are handled by the existing batch cleanup; shared COM aliases are not force-released.

## Existing limitations

Bushing projection relationship recognition is incomplete; its interpart-link check alone does not certify all profile associativity. Native selectors verify dropdown values separately from feature validation. A previously loaded part can block SaveAs even if its on-disk file was removed. Preserve unsaved work before retrying.
