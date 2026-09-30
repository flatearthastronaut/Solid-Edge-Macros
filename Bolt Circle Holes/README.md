# Bolt Circle Holes v0.18

Run `Compiled Executables\BoltCircleHoles-v0.18.exe` with a part open in Solid Edge 2026. Keep the included `C'bore Chart.xls` beside the executable. Select the hole type/size and quantity (the **6** button selects six), select the Create From face or reference plane, and click the first center. Inspect the result before saving the part; the macro never saves it.

Inch and metric thread lists contain the 26 sizes from `Thread_Depth_Charts.pdf`. Full-thread and drill-to-shoulder depths use that chart's inch columns. Each placement creates ONE native Hole feature containing all requested threaded holes, equally spaced around base Z starting exactly at the clicked point. Thread placement adds no construction lines, dimensions, or separate circular Pattern feature. For multiple threads, the selected support must be perpendicular to base Z and the first center must be off the axis. The 120-degree drill point extends beyond the chart's shoulder depth.

Counterbores, A2 presets, and button holes retain their existing dimension and native circular-pattern behavior. All displayed linear dimensions are inches. Matching bundled counterbore charts load without Excel or ACE.

## v0.18 thread correction

Solid Edge 2026 silently returned zero dimensions from the legacy ThreadDataByDescription lookup, causing the reported 1/4-20 UNC error. This release uses HoleDataCollection.AddEx with ANSI Inch / UNC / 2B or ISO Metric / Metric / 6H. It checks the returned thread identity, nominal size, and tap-drill diameter because an unrecognized table size can silently select the table's first entry.

HoleDiameter stays at the nominal thread diameter; ThreadDiameterOption selects the tap drill. Setting HoleDiameter to the drill diameter would change or clear the thread selection. Profile.End splits the disconnected centers into profiles in one ProfileSet. After creating a seed hole, UserDefinedPatterns.AddByProfiles consumes it into the native multi-profile Hole group, displayed as ONE Hole entry in Pathfinder. This is Solid Edge's internal representation of an Ordered multi-hole feature; there is no separate circular Pattern or set of individual Hole features. Automatic profile dimensions are explicitly removed. Feature status, occurrence count, material removal, depths, final collection counts, and absence of a new circular Pattern are checked; failures trigger ownership-aware rollback. Only thread selections use this grouping; counterbore and A2 behavior is unchanged.

Validation: compiled against installed Solid Edge 2026. All 26 thread table entries and their configured depths passed live checks. A temporary unsaved test part successfully received six 1/4-20 UNC holes in ONE Hole feature and six M10 holes in ONE Hole feature. All twelve retained their selected thread identity, with zero profile dimensions and zero circular Pattern features. Occurrence counts, six new physical hole geometries per feature, and analytical material-removal volumes verified the complete groups. An off-body placement was rejected and rolled back while preserving the existing groups and body. The scratch part was closed without saving. Chart, unit conversion, spacing, selection switching, and quantity-shortcut regression checks are in tests/VerifyThreads.ps1. The repeatable live test is tests/VerifyNativeThreads.cmd (source: tests/VerifyNativeThreads.cs); it requires a running Solid Edge instance and creates/closes its own unsaved part.

Loose source, charts, executable, and the ZIP release remain in this folder; the executable and ZIP are also in Compiled Executables. Build.cmd rebuilds the executable. View log shows the persistent BoltCircleHoles-run.log, including thread-table selection, returned values, each created position, validation, and cleanup details.

## Historical release notes

The sections below record earlier releases and their validation limits; the v0.18 instructions above supersede earlier run instructions and thread implementation descriptions.
# Bolt Circle Holes v0.7

Creates one native counterbored Hole feature from the selected chart size. The drill extent is always Through All. Bolt-circle patterning is a later increment.

## Run

1. Close an older version of the macro. Open the target .par in Solid Edge 2026 and finish any active modeling command.
2. Run BoltCircleHoles-v0.7.exe directly or with Solid Edge Run Macro.
3. Choose a screw size. The macro shows the chart's drill diameter, counterbore diameter, and counterbore depth in inches.
4. Click Select face / plane and select the flat face or reference plane to use for Create From.
5. Click the hole center in the graphics view. For a flat face, click on that same face. The macro creates the counterbored through hole immediately.
6. Inspect the result and save the part yourself when satisfied. The macro does not save it.

For flat faces, the initial cutting direction is into the body. For reference planes, it uses the positive profile-normal direction. Reverse cutting direction flips this choice and can be set before clicking the center. Right-click or Cancel during picking cancels without creating a feature. A reference plane can be used with a single-body part; in a multibody part, select a flat face on the target body.

## Chart and files

C'bore Chart.xls beside the executable is the source, Sheet1 columns A:D: SCREW, DRILL, C'BORE, DEPTH. All dimensions are inches, including metric screw rows, as confirmed by the user. Values convert to Solid Edge's meters by multiplying by 0.0254.

All 26 screw sizes appear in chart order. M3 X 0.5, #4, and #5 have blank depths and cannot create holes until the depth is supplied in the chart. No default depth is invented. Restart after chart changes. The macro reads the chart without changing it.

Loose project files and BoltCircleHoles-v0.7.zip are in this folder:
- BoltCircleHoles-v0.7.exe: run this version.
- BoltCircleHoles.cs: chart reader and selection window.
- HoleEngine.cs: profile and Hole feature creation.
- RunLog.cs: timestamped diagnostic logging and live log window.
- Build.cmd: rebuild against installed Solid Edge 2026 libraries.
- C'bore Chart.xls: size standard.
- README.md: these instructions.

Requires 64-bit .NET Framework 4.x, Solid Edge 2026, and the installed 64-bit Microsoft ACE OLE DB provider. The macro does not open Excel.

## Behavior and recovery

A flat face gets a zero-offset local profile plane parented to the selected face, retaining that Create From support. The hole profile contains one Holes2d center. The engine uses counterbore HoleData with the profile at the top and adds a Through All Hole to the selected model. It creates an Ordered feature and attempts to restore the previously active modeling mode afterward.

The macro checks the Hole status, solid validity, material removal, and the returned drill/counterbore dimensions before reporting success. Failed creation attempts clean up objects created by that attempt. If ownership or cleanup cannot be established, the error explicitly says partial work may remain; inspect the part before trying again. Errors are recorded in BoltCircleHoles-error.txt. No document is automatically saved or closed.

Retains the v2.29 SketchToModels lessons: 64-bit STA execution, bounded OLE busy retries, a single-instance guard, typed geometry calls, original-document checks, and persistent errors. A face-center pick uses PointOnGraphic during the mouse event so it captures the actual face point, rather than an unrelated view-plane point. Reference-plane centers use view-ray/plane intersection; edge-on views are rejected.

## Validation

Compiled without warnings against installed Solid Edge 2026 interfaces. Checked chart loading, 1/4-inch dimensional conversion (0.007112 m drill, 0.010414 m counterbore, 0.007112 m depth), missing-depth blocking, flat/angled plane intersection, edge-on rejection, and the rendered popup layout.

Live CAD validation was not possible: the Solid Edge automation connection returned MK_E_UNAVAILABLE in this session. Native hole creation, inward direction, face association, cleanup, and graphics selection therefore remain unverified in Solid Edge. Test one hole before continuing development.

API references:
- https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~Holes~AddThroughAll.html
- https://support.industrysoftware.automation.siemens.com/trainings/se/106/api/SolidEdgePart~HoleData~CounterboreDepth.html
- https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~RefPlanes~AddParallelByDistance.html
- https://support.industrysoftware.automation.siemens.com/trainings/se/106/api/SolidEdgeFramework~Mouse~PointOnGraphic.html

## v0.4 diagnostics

Close v0.3 before running BoltCircleHoles-v0.7.exe. Click View log for an automatically refreshing window. Uncheck Live updates to inspect/copy a section without scrolling. Copy log copies the visible recent log text. The full file is BoltCircleHoles-run.log beside the macro; it appends across runs, with session identifiers and timestamps. The viewer shows the latest 256 KB; the saved file retains the full history. BoltCircleHoles-error.txt is also retained for compatibility.

The log records connection and document details, chart loading, selected size, support and mouse clicks, actual center coordinates, inch/meter dimensions, profile coordinates, face and profile normals, chosen cutting direction, each creation stage and its timing, feature status name and numeric value, returned dimensions, volume checks, cleanup, COM busy retries, and exceptions with HRESULT/stack traces. No periodic mouse-move logging is used. Errors also show in a full dialog so long messages are not clipped.

The 10:18 screenshots of ADAPTER 21930.par show successful flat-face selection and center picking with the 3/8 size (0.410 drill, 0.600 counterbore, 0.410 depth). The existing error log shows non-OK feature status, followed by disconnected-object errors during cleanup. The previous code did not record the status value or creation inputs, so the reason for the hole rejection is not yet established.

Cleanup now distinguishes a cascade-deleted child from a new failure: a disconnected object is treated as already removed only when its owning collection can be read and is back to its original count. Genuine cleanup failures remain visible. The original creation error is logged before cleanup starts. No change to the chosen hole geometry or direction was made solely on the basis of the screenshots.

Validation: build passed without warnings; the run log and both windows were inspected; synthetic tests verified successful-stage timing, full HRESULT/error recording, original exception preservation, and both verified and unverified disconnected-child cleanup. Earlier conversion, missing-depth and plane-intersection checks also passed. These checks do not verify live Solid Edge hole creation. Reproduce once in v0.4 and inspect the saved run log to diagnose the feature rejection.

## v0.5 correction: valid HoleData treatment

The reverse-direction attempt also returned igFeatureFailed. Inspection of the HoleData API contract found a code error: v0.3/v0.4 supplied igTreatmentOff (105), whereas an untreated hole requires igNone (44). Valid HoleData treatment choices are igNone, igTappedHole, and igTaperedHole.

v0.5 explicitly uses igNone and verifies that Solid Edge returns igCounterboreHole and igNone before creating the feature. The log now includes both returned values and the numeric treatment value. The existing chart dimensions, Through All extent, center selection, and cleanup behavior are retained.

Validation: compiled without warnings against Solid Edge 2026; verified that igNone=44 and igTreatmentOff=105 in the installed type library, and checked the API's allowed treatment values. This corrects a documented invalid argument. Successful live hole creation still requires confirmation. Close the previous macro, run v0.5, and start with Reverse cutting direction unchecked.

Reference: https://support.industrysoftware.automation.siemens.com/trainings/se/106/api/SolidEdgePart~HoleDataCollection~Add.html (TreatmentType parameter).

## v0.6: explicit support plane for each hole

The v0.5 run log confirms successful M10 X 1.50 hole creation at 10:31:58 on September 22: igFeatureOK, valid remaining solid, volume reduced by 3.39210183867992E-06 m3, and chart dimensions verified (0.420 drill, 0.650 counterbore, 0.390 depth in inches). Reverse was unchecked. The earlier click at 10:31:44 returned an unsupported object and was rejected before modeling.

Subsequent 1/4-inch attempts failed a different check: the selected face/center was at Z=0.03683 m, but profile conversion returned Z=0. No hole was cut in those failed attempts. The successful M10 hole remained in the model.

v0.6 creates a separate, explicit zero-offset plane parented to the selected face instead of a command-local plane. It verifies plane height/orientation before creating the profile, retains the profile-to-center round-trip check, and avoids assigning Ordered mode when already in Ordered mode. The face-linked plane is hidden after successful creation. The log includes both face and generated-plane roots/normals.

Validation: build passed without warnings. Tests accepted coincident planes with opposite normals and rejected the logged 0.03683-to-0 height mismatch, a rotated plane, and a zero normal. The support change targets the observed second-hole failure; live repeat-hole behavior remains unverified.

## v0.7: construction line to base CSYS Z

Each new hole profile receives a construction line from its hole center to the part's base coordinate system Z axis. For a horizontal face at height z, the axis endpoint is (0, 0, z), not the part origin below the face. For a tilted plane, the endpoint is the plane/Z-axis intersection. If the plane contains the Z axis, the nearest point on that axis is used.

The line is marked as a true construction element with Profile.ToggleConstruction and checked with IsConstructionElement before feature creation. Its start is related coincident to the Hole2d center; its axis endpoint is fixed in profile coordinates. This lets the hole center move within the profile while the radial line stays attached. The fixed endpoint is not a projected associative 3D-axis reference: changing the supporting plane's tilt later may require updating that endpoint.

The line is part of the hole's feature profile. Edit/show that profile to see it; the completed profile is hidden in normal model view as before. Existing holes are not retrofitted. A hole already on the Z axis does not receive a zero-length line. A profile parallel to Z but offset from it cannot contain a line to Z; that case is rejected before hole creation, with cleanup and a clear message.

CONSTRUCTION entries in BoltCircleHoles-run.log record the hole endpoint, axis endpoint, construction conversion, endpoint relationships, and any failures. Chart sizes, Through All hole behavior, and face selection are unchanged.

Validation: compiled without warnings. Endpoint calculations passed for an elevated horizontal plane, tilted plane, and plane containing Z; a parallel offset plane was rejected. Construction geometry and coincidence/fix relationships are compiled against the installed Solid Edge interfaces but still need live CAD validation.

API references:
- https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~Profile~ToggleConstruction.html
- https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Relations2d~AddKeypoint.html

## v0.8 - Construction-line position dimensions

Adds two driving dimensions in the hole profile: the radial construction line's full length and its angle to a projection of the part's actual base Right (YZ) reference plane. Edit the hole profile to view/change these dimensions. The profile remains hidden after feature creation, as in previous releases. Length display follows the part's dimension style/units; chart dimensions still convert from inches.

The base Right plane is identified geometrically among the three base planes, following the SketchToModels lesson of avoiding a hard-coded Right-plane index. The projected reference is construction geometry. Dimension values are captured from the picked position rather than setting a new position. The macro verifies the length, driving flags, and unchanged line endpoints. DIMENSION entries in the existing log record projection, creation, length in inches, and angle in degrees. A failed dimension rolls back the owned hole profile through the existing cleanup.

Solid Edge's Profile.ProjectRefPlane requires the Right plane to be perpendicular to the hole profile. Other orientations are rejected with an explanation before a hole is created. A hole exactly on base Z still has no construction line and therefore no position dimensions. The axis endpoint remains fixed in profile coordinates, as in v0.7.

Validation: compiled successfully against the installed Solid Edge 2026 interfaces. Live creation and dimension editing must be verified in Solid Edge; this session cannot access the running application's COM instance.

API references:
- https://support.industrysoftware.automation.siemens.com/trainings/se/106/api/SolidEdgePart~Profile~ProjectRefPlane.html
- https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeFrameworkSupport~Dimensions~AddAngleBetweenObjects.html

## v0.9 - Preserve the projected Right-plane reference

The v0.8 run at 10:46 on September 22 successfully projected the Right plane, then failed in Profile.ToggleConstruction(reference), HRESULT 0x80040223. Cleanup removed the new profile set and support plane. The error's generic Windows text referred to TabletPC inking, but the recorded failing operation was the construction conversion.

Removed that conversion and its construction-state requirement for the projected plane. The angular dimension now uses the returned reference directly. The radial line remains construction geometry. This corrects the v0.8 note that the projected reference would be converted to construction. Added separate log entries for driving flags and dimension placement so any subsequent failure identifies its precise operation.

Validation: built against installed Solid Edge 2026 interfaces. The failing conversion is absent from the dimension path. Live dimension creation still requires a run in Solid Edge.

## v0.10 - Equally spaced holes around base Z

The window now has Number of holes (1-999, default 1) and a live spacing summary. The number is the TOTAL including the first picked hole: 6 creates six holes at 60-degree intervals. A count of 1 creates the existing single hole with its radial and angular dimensions. Select the size and count, pick the Create From face/plane, then click the first hole center as before.

For counts above 1, creates a native circular Smart Pattern of the finished through counterbore using Patterns.AddByCircularEx. The base XY reference plane is identified geometrically, and the axis point is the base origin, so the pattern rotates around base Z regardless of the selected face's height. The full-circle pattern uses fixed spacing of 2*pi/count radians and includes the original hole. A center on Z is rejected for multiple holes.

The count is locked during selection. Logging includes the requested count/spacing/axis, API call, feature status, returned circular pattern count and spacing, occurrence count, and before/after volume. Success requires an OK feature, matching count/spacing, and additional material removal from a valid solid. Failure removes the owned pattern before the seed hole and its profile; ambiguous ownership stops cleanup and reports the issue. The part remains unsaved. Existing face-orientation limits for the Right-plane angle dimension still apply.

Validation: compiled using installed Solid Edge 2026 interfaces; checked count boundaries and full-circle spacing; rendered and inspected the window with 6 holes/60 degrees; verified the missing-chart-depth guard. Live pattern creation is not yet verified in Solid Edge.

API reference: https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgePart~Patterns~AddByCircular.html (installed AddByCircularEx additionally accepts Smart/Fast pattern type).

## v0.11 - Keep the successfully created circular pattern

The v0.10 log at 10:52:10 on September 22 shows AddByCircularEx succeeded for 6 holes at 60 degrees and returned igFeatureOK. The following GetCircularPatternData call threw 0x80004021 (operation not supported), triggering rollback of the otherwise valid pattern and seed hole.

Removed that unsupported post-creation readback. Pattern creation still receives the requested total count and exact 2*pi/count spacing. Feature status and additional material removal from a valid solid remain required. The log labels count and spacing as supplied inputs, not independently read-back values. This supersedes the v0.10 readback validation description. The hole, dimensions, pattern creation method, and selection workflow are unchanged.

Validation: compiled against installed Solid Edge 2026 interfaces. The failing GetCircularPatternData call is absent. The prior live log confirms creation returned igFeatureOK; this corrected release still needs a live run to confirm the retained result.

## v0.12 - Portable chart loading on other machines

The supplied September 23 logs from the other computer show successful connection to Solid Edge and the active part. Startup failed in Chart.Load because Microsoft.ACE.OLEDB.12.0 was not registered for the running 64-bit macro. No hole creation was attempted.

The executable now embeds all 26 rows exported from the original C'bore Chart.xls, with a SHA256 fingerprint of that workbook. When the workbook beside the executable matches, its embedded sizes load without ACE or Excel. All values remain inches, and the three missing depths remain blank and blocked. Keep the matching XLS beside the executable. CounterboreChart.xml is a build resource; users do not need to edit it.

If the workbook changes, embedded values are not used. The macro attempts the original workbook reader so edited charts still work on machines with ACE. If the reader is unavailable, an explicit message asks for the matching release chart or a refreshed portable build. Do not change the embedded fingerprint alone: a refresh must re-export all rows from the revised workbook and rebuild the executable.

To use on the other machine: extract BoltCircleHoles-v0.12.zip into the macro folder, keep the included XLS beside the EXE, and run BoltCircleHoles-v0.12.exe with a part open. No Solid Edge hole, dimension, or pattern behavior changed.

Validation: successful build; all 26 rows and each screw/drill/counterbore/depth value compared with the direct spreadsheet reader; all three missing depths preserved; altered workbook rejected by the portable path; preview and missing-depth UI check passed. The matched-chart path was exercised locally without invoking the workbook reader. The other machine itself has not yet been retested.

## v0.13 - Inch, metric, and A2 counterbore selections

Three dropdowns now separate inch screw sizes, metric screw sizes, and A2 C'bores. Choosing a standard size clears the A2 preset; choosing an A2 entry populates its metric screw size, drill diameter, counterbore diameter, depth, and Z radius. All displayed linear values remain INCHES, including metric screw entries. Number of holes remains the user's choice because the PDF does not give a mounting-hole count.

A2 mappings are transcribed from A2 Adapter Chart.pdf (2/27/2026), using its S.H.C.S. column to select the existing counterbore chart row and its Z column as radius:

| Adapter | Screw | Z radius (in) | Drill (in) | Counterbore (in) | Depth (in) |
| --- | --- | --- | --- | --- | --- |
| A2-5 | M10 X 1.50 | 2.063 | 0.420 | 0.650 | 0.390 |
| A2-6 | M12 X 1.75 | 2.625 | 0.500 | 0.730 | 0.500 |
| A2-8 | M16 X 2.00 | 3.375 | 0.670 | 1.020 | 0.660 |
| A2-11 | M20 X 2.50 | 4.625 | 0.830 | 1.300 | 0.820 |

The PDF's Button Hole Dia / Dp columns refer to the drive-button hole and are not mounting-counterbore dimensions. A2-15 and A2-20 select M24 but cannot create a hole because Z is blank. A2-28 also lacks Z and an M30 counterbore row in the size chart. These entries display the missing data and block creation; no sizes are guessed.

For A2, choose a face/reference plane perpendicular to base Z, then click on that support to set the angular position. The macro moves the center radially to the exact chart Z radius while retaining the clicked angle and support elevation. The existing construction-line length dimension therefore receives the chart radius. An on-axis click and a nonperpendicular support are rejected. The first hole and native circular pattern are then created using the existing validated flow. Standard inch/metric selections retain free clicked-center placement. A2.center logging records both the click and resolved coordinates.

A2 values are bundled in A2Chart.cs; the PDF is included for reference, not parsed at runtime. The portable XLS chart loader is unchanged and still requires no ACE/Excel for the matching workbook.

Validation: compiled successfully; checked all 26 standard rows split into inch/metric, all 7 A2 entries, all four complete A2 dimension mappings, blocked incomplete entries and missing depths, switching back to standard selection, radius/angle preservation in four quadrants, rejected invalid A2 placement, and rendered/inspected the window. Live Solid Edge creation with the new A2 radius placement remains to be tested.

## v0.14 - A2 Holes includes counterbores and button holes

Renamed the dropdown to A2 Holes. Each adapter has a Counterbore and Button Hole option (14 entries total). Counterbores retain the existing through-all behavior and dimensions. Button holes use a regular, untapped, flat-bottom finite-depth Hole feature, with diameter/depth from the PDF's Button Hole Dia / Button Hole Dp columns, in inches:

| Adapter | Button diameter | Blind depth | Z radius |
| --- | --- | --- | --- |
| A2-5 | 0.640 | 0.250 | 2.063 |
| A2-6 | 0.770 | 0.250 | 2.625 |
| A2-8 | 0.960 | 0.310 | 3.375 |
| A2-11 | 1.150 | 0.380 | 4.625 |
| A2-15 | 1.400 | 0.380 | Not provided |
| A2-20 | 1.650 | 0.380 | Not provided |
| A2-28 | Not provided | Not provided | Not provided |

Incomplete rows remain visible but blocked. The chart's 15-degree mounting-hole offset note for A2-15 through A2-28 is shown with the incomplete button entries; the macro does not guess a mounting-hole reference or a missing radius. For supported entries, choose the appropriate entry face and click to set the angular position. The chart fixes the radius. Count remains user-controlled (1 for a single button hole); counts above 1 use the existing circular pattern flow.

The button-hole path verifies diameter, finite extent, blind depth, flat bottom, feature status, and material removal. It logs the new AddFinite operation and checks. No spreadsheet driver is required for the matching bundled standard chart.

Validation: successful build against Solid Edge 2026, all 14 options checked, all provided button diameters/depths compared with the PDF, incomplete options blocked, existing counterbore dimension validation retained, unit conversions checked, and button-hole UI rendered/inspected. Live button-hole creation has not yet been tested.

## v0.15 - 120-degree button-hole bottom and compact window

Button holes now use a 120-degree included V bottom. The chart depth is explicitly measured to the end of the full-diameter cylindrical portion (igVBottomDimToFlat); the drill point extends beyond it. Counterbores retain their through-all behavior. The post-creation check expects a 120-degree bottom angle.

Reduced the window client height from 645 to 506 pixels (about 22%) by tightening vertical spacing, shortening the header, and removing a redundant instruction label. All three selectors, dimensional summary, direction checkbox, count, status, and buttons remain visible with the same 10-point body font.

Validation: build passed; A2 table and UI checks passed; compact button-hole preview rendered and visually inspected. New V-bottom geometry still requires live Solid Edge verification.

## v0.16 - Thread placeholders and hole pictures

Added Metric Threads and Inch Threads dropdowns marked Coming soon. They are disabled, have no selection handlers, and do not participate in hole creation. Existing hole selections and geometry are unchanged.

Each selection row now includes a small scalable section symbol. Inch and metric counterbores use a stepped through-hole symbol; the A2 symbol switches between counterbore and V-bottom button hole. Thread placeholders have a threaded section symbol. Symbols are drawn locally with disposed drawing resources, with no external image dependency. The compact window is 510 by 574 pixels to accommodate the two new rows.

Build.cmd now also places the executable and required XLS in Compiled Executables. The ZIP and loose files remain available in the macro folder. Validation: compiled, preview inspected, and tests/VerifyPlaceholders.ps1 passed for disabled placeholders, A2 icon changes, reset behavior, and control bounds.

## v0.17 - Blind threaded holes and quantity shortcut

Metric Threads and Inch Threads now contain the 13 metric coarse and 13 inch UNC entries from Thread_Depth_Charts.pdf (DB, 9/29/2026). Both full-thread and drill-to-shoulder depths use the PDF's INCH columns consistently; its independently rounded millimeter columns are not mixed into the calculation. The new 6 button sets the total quantity to six with one click and is locked with the quantity field during placement.

For THREAD selections only, the first hole center is exactly where clicked. Remaining centers are equally spaced about the part's base Z axis at the same radius and elevation. The macro adds explicit Hole2d centers to one finite tapped Hole feature. It adds no sketch dimensions, construction line, Right-plane projection, or Pattern feature. Counts above one require a support perpendicular to base Z and a click off that axis. Other counterbore/button-hole behavior remains unchanged.

The PDF provides sizes and depths, not tap-drill diameters. ThreadDataByDescription resolves the selected size in the running Solid Edge thread table with explicit metric/inch units. A bounded set of common description spellings is tried. The native nominal size and tap-drill diameter are checked; failure reports THREAD.lookup details and does not substitute a clearance diameter. The thread uses native igTappedHole treatment, finite thread depth, tap-drill diameter, and the chart's finite drill depth to the full-diameter shoulder. A 120-degree drill point extends beyond the shoulder, consistent with the macro's current drill-point setting. These are native tapped-hole definitions, not modeled helical grooves.

Logging records lookup attempts, thread settings, every additional center, count/spacing, and feature readback. Existing feature-status, material-removal, ownership-aware rollback, and unsaved-part behavior remain. Post-creation validation checks finite thread/drill depths, tapped treatment, zero profile dimensions, and no new Pattern feature.

Validation: built against Solid Edge 2026; tests/VerifyThreads.ps1 checks all 26 chart rows against the chart's rounding formulas, unit conversion, 1/2/6/13/999 center counts and spacing, unchanged first point/elevation/radius, rejected invalid multi-hole supports, dropdown exclusivity, and the 6 shortcut. Window rendered and inspected. Live thread-table lookup and native tapped-hole creation still require testing in Solid Edge.

API references: Solid Edge SDK SolidEdge.Part.Holes sample for profile/Holes2d/AddFinite flow; Siemens HoleData.ThreadDataByDescription documentation for explicit lookup units, ThreadDiameterOption for tap-drill choice, and HoleData.ThreadDepth for finite thread-depth semantics.
