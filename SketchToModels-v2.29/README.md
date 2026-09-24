## v2.29 — existing styles only; diaphragm yellow

Supersedes v2.28 color behavior: never creates or modifies a color style. Reuse the matching existing style; if absent, skip coloring that part and continue saving/processing. Diaphragm and diaphragms now match Yellow, as does collet. All other name/color rules remain unchanged.

Build and tests passed for nine name cases, existing style preservation, body/feature application, missing-style skip without creation, and unmatched names.
## v2.28 — automatic Part Painter colors

After completing the protrusion and retaining the assembly sketch link, apply the matching face style before saving:
- collet -> Yellow
- part stop / partstop -> Green
- adapter -> Bronze
- puller -> Orange
- body -> Khaki

Names match case-insensitively by contained text, ignoring spaces, hyphens, and underscores. Unmatched names retain their style. Existing named face styles are reused without modifying them. Missing styles are created with standard RGB colors (Bronze 205/127/50). Applies to the part base, model bodies, and revolved protrusions through the style API; it does not open the Part Painter dialog. Applies to both standard and Bushing Style workflows.

Validation: build passed; simulated tests cover name matching, style reuse, fallback creation, body/feature application, and unmatched names. Existing Continue handling test passed. Live appearance in Solid Edge has not been verified.

API references:
https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~PartDocument~SetBaseStyle.html
https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeGeometry~Body~Style.html
https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFramework~FaceStyles~Add.html
## v2.27 — unblock Bushing Continue and retain error messages

The actual v2.26 log showed Continue reached solid validation but stopped on an unrecognized projection relationship. Bushing Style now logs this as an associativity warning after checking that the part still has an interpart link. It still requires a valid 360-degree solid on Right, the expected sketch count, and the saved assembly sketch link. This does not certify that the solid profile updates associatively.

Clicking Continue also stops the background completion status from overwriting validation errors. Standard workflow projection checks remain unchanged.

Build, Continue handling with a stalled selection worker, and incomplete-feature guards passed. Live CAD verification is still required.
## v2.26 — final Continue advances the batch

After Finish on the bushing protrusion, Continue validates the solid, exits the active feature command, and closes the instruction window. The existing batch flow then saves the part and assembly and processes the next selected part, or reports completion when none remain. An open sketch or invalid solid keeps the window open.

Build, blocked-search Continue handling, and incomplete-feature validation checks passed. Live Solid Edge handoff remains to be confirmed.
## v2.25 — Continue starts the solid revolve

After finishing the 360-degree revolved surface, click Continue after Finish. The macro validates the surface, exits any still-active surface command, waits for the command change, then opens the revolved protrusion on Right (yz). A still-open sketch or missing/invalid surface does not advance.

Build and surface projection / Z-axis regression checks passed. The live Solid Edge command handoff still needs confirmation.
## v2.24 — automatic surface Z centerline

Before starting Project to Sketch / Wireframe Chain in the Bushing Style surface feature profile, the macro defines and verifies its revolution axis along the part's Z axis. The part's identity placement coordinate system is aligned to the assembly coordinate system chosen during placement, so this is the selected coordinate system's Z axis. The profile must contain this axis; an incompatible plane stops projection assistance with a message.

Checks passed: simulated axis creation/verification and retry guards, surface-profile detection and projection startup with axis assignment, and native Wireframe Chain dropdown selection. Live Solid Edge validation is still needed. The subsequent bushing solid step retains its user-picked axis.
## v2.23 — surface sketch projection

Bushing Style now waits for entry into the surface feature profile, then starts Project to Sketch and selects Wireframe Chain using the native dropdown selector. Pick the original outline and accept, then choose the centerline, close the sketch and Finish the 360-degree surface. The solid revolve stage follows as before.

Checks passed: feature-profile guard, rejection of original sketch editing, one-time projection launch without setting an axis, separate-process native ComboBoxEx selection for Wireframe Chain and Feature's Plane, and existing bushing solid projection launch. Solid Edge testing is still needed.
## v2.22 — correct ComboBoxEx option text

The v2.21 log found Create From but read truncated option labels from its inner owner-drawn ComboBox. v2.22 reads option text through the ComboBoxEx wrapper, matches the full Feature's Plane label, selects it, and notifies the command panel.

Regression test: a separate process hosts a native ComboBoxEx in an owned floating window. v2.21 fails this test; v2.22 passes. Regular floating dropdown notification, placement lookup, and manual-axis projection checks also pass. Live Solid Edge confirmation is still needed.

Close the previous macro and run SketchToModels-v2.22.exe. Test one Bushing Style part.
## v2.21 — floating Create From panel

Uses the control report from Solid Edge to find dropdowns in floating panels as well as the main window. Selects Feature's Plane by its label and notifies the parent of the ComboBoxEx control. Removes the potentially stalled UI Automation fallback for this step.

Validation: built successfully; native selection and selection-change event passed with both regular and owned floating test windows. Coordinate-system lookup and manual-axis projection checks passed. Solid Edge end-to-end validation still requires a user test.

Close the previous macro and run SketchToModels-v2.21.exe. Test one Bushing Style part. The Revolved Surface Create From field should switch to Feature's Plane automatically.
Version 2.20 adds a native Windows combo-box selection path before the UI Automation fallback. It enumerates visible, enabled same-process combo controls, reads option text, selects Feature's Plane by exact normalized label, sends selection-change/commit notifications, and verifies the selected index. Calls have bounded waits. A real WinForms combo-control test passed for selection and notification. Actual Solid Edge acceptance remains unverified. SketchToModels-plane.txt logs the exposed native option names and results.

Version 2.19 improves Bushing Style Create From selection. It waits/retries as the Revolved Surface panel appears, targets plane-selection controls, searches same-process popup lists as well as the main window, accepts straight/curly apostrophes, and verifies the combo value after selecting Feature's Plane. A Set Create From to Feature's Plane button allows retry. Search remains on a background worker. Build and existing placement/axis regression checks passed; popup rendered and inspected. Actual Solid Edge dropdown selection still needs live verification.

Version 2.18 replaces the name-indexed bushing placement-origin lookup with numeric enumeration and an exact name match. This addresses the suspected Invalid index failure seen after selecting CSYS BUSHING. Grounding on the new occurrence is released after the lookup succeeds. Placement errors now include the operation stage and are logged in SketchToModels-placement.txt. Build and simulated numeric-lookup tests passed; placement still needs live verification.

# Sketch to Models v2.17 — Bushing Style

Close any older macro before running SketchToModels-v2.17.exe. Open a saved assembly. Check Select for the established workflow, or Bushing Style for the new workflow. Checking Bushing Style is enough to include that row; if both boxes are checked, Bushing Style takes precedence. Clear clears both columns.

## Bushing Style workflow

1. A part is created from normal.par beside the assembly, using the same sketch/drawing-number filename. A coordinate system at the new part origin is added for placement.
2. In the assembly, select the destination coordinate system graphically or in PathFinder, then click Use selected coordinate system. The macro aligns the new part's coordinate system using an assembly coordinate-system relationship. Only grounding relationships on this new occurrence are suppressed to allow placement. Selection must be a coordinate-system object or a reference to one; selecting an axis alone is rejected.
3. The original assembly sketch is copied associatively into the placed part using the existing CopySketch sequence.
4. Revolved Surface starts. The macro attempts to select Feature's Plane in Create From. Choose it manually if Solid Edge's control is not exposed to automation, then pick the copied sketch. Sketch and select your centerline, close the sketch, set a full 360-degree revolution, and Finish.
5. Once the surface is valid and Solid Edge is back at Select, the macro starts Revolved Protrusion on Right (yz). Project to Sketch / Wireframe Chain is launched in the feature profile. Pick the geometry and choose your own revolve axis; no automatic Z centerline is added in Bushing Style.
6. Close Sketch, use 360 degrees, and Finish. If Solid Edge stays in the feature command, press Esc to return to Select. The macro checks the solid, saves the part and assembly, and proceeds to the next selected item automatically. Continue provides an explicit check if needed.

The standard workflow retains the automatic Z centerline and its existing Continue/save behavior. Existing filenames remain blocked. Cancel preserves partial parts and stops the batch.

## Verification

Compiled against the installed Solid Edge 2026 interfaces. Existing naming/plane checks, manual-feature guards, blocked-search responsiveness and save-handoff checks passed. A simulated bushing test confirmed that Project to Sketch launches without calling automatic axis setup. Both new modeling popup layouts were rendered and inspected.

This new workflow is not yet live-validated. Test one bushing before a batch. Coordinate-system placement, associative sketch behavior under the chosen placement, Feature's Plane selection, surface completion, and automatic transitions all need verification in Solid Edge. Failed processing leaves the part available for inspection. Prior version v2.16 remains available.

Source and Build.cmd are included. The template setting is retained in template.txt. SketchToModels-error.txt records processing failures; SketchToModels-assist.txt records assisted protrusion messages. The helper uses Windows UI Automation for command options and may require a manual option selection if Solid Edge does not expose the control.












