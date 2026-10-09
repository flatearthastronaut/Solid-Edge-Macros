# Populate SHCS v1.0

Run **Compiled Executables/PopulateSHCS-v1.0.exe** with the main assembly active in Solid Edge 2026. This standalone 64-bit macro does not need add-in registration.

## Use

1. Finish Edit In Place and activate the assembly components. The Standard Parts folder is read from your Solid Edge Standard Parts configuration; Browse can change it.
2. Click **Scan assembly**. Review the component, screw size, measured cylinder length, calculated length, selected length and status. Ready rows are checked automatically; uncheck any you do not want to populate.
3. If a required size is missing, select its row and click **Choose missing Standard Part**. The native Standard Parts selector opens. Choose an SHCS of the displayed diameter and the next available length at or above the requirement. For filenames outside the verified ANSI convention, enter the chosen catalog length in inches. The macro verifies the model's diameter, under-head length and mounting geometry and remembers that selection.
4. Click **Insert checked screws**. Inspect the assembly and save it yourself when satisfied.

Each screw is added as a top-level occurrence in the main assembly, even when its hole is in a nested subassembly. Its underside is seated against the counterbore shoulder and its shank points down the smaller cylinder. This version uses **native Ground relationships**, not associative axial/mate relationships. Screws remain at their placed positions if the source components later move.

## Sizing

The executable embeds this repository's `Bolt Circle Holes/CounterboreChart.xml`, the existing transcription of `C'bore Chart.xls`. All chart dimensions are inches, including metric entries. Both counterbore diameter and clearance-cylinder diameter must match within 0.001 inch. Inch entries win a chart tie (notably #10 versus M5). A nearby bore size alone is not sufficient.

Under-head screw length is:

`nominal screw diameter × 1.5 + measured straight cylinder length below the shoulder`

The macro rounds up to the next **available indexed part length**, never down. Example: a 1/4-inch screw with a 0.500-inch pilot cylinder requires 0.875 inch, so it selects 7/8 inch if available. A 0.510-inch cylinder requires 0.885 inch and selects 1 inch when that is the next indexed length.

The automatic index recognizes generated **ANSI B18.3.A hexagon socket-head cap screws**, including numbered and mixed-fraction filenames used by your configured Standard Parts library. It does not confuse button heads, flat heads, shoulder screws or variable parent templates with SHCS. Metric and other naming schemes use the native selector and remembered mappings. The selector can generate missing parts through the installed Standard Parts COM interface. Automatic indexing does not query or modify the catalog database or generate every possible size. If a shorter standard size exists only as an ungenerated catalog entry, use the selector to add it before inserting.

The actual selected part is measured before insertion: nominal diameter, under-head length, head diameter, complete head height, underside and direction must all be verified. Cosmetic-thread library models are supported. Fully modeled helical threads or unusual geometry may require a different/simplified library representation.

## Recognition and recovery

- Scans all readable solid bodies and component instances, including hidden components and nested suboccurrences. Part geometry is read once per distinct source document per scan; occurrence transforms are applied separately.
- Requires two complete coaxial internal cylindrical faces and their common flat shoulder. Imported models are supported when this topology is intact. Interrupted, split, chamfer-separated, faceted or noncylindrical hole geometry may not be recognized. Blind/interrupted exits and opposed counterbores are flagged for manual review.
- Detects existing coaxial cylindrical hardware. A conservative clearance-box test flags other nearby components for manual review, so some empty holes may be skipped. It does not pretend that an ambiguous obstruction is an empty hole.
- Unreadable/inactive components, unsupported mirrored/scaled transforms, adjustable parts and body overrides block automatic insertion because occupancy cannot be established for the complete assembly. The issue list identifies each component.
- Scans again immediately before insertion. A changed assembly invalidates the review. Failed or cancelled insertion rolls back only occurrences created by that batch; a cleanup failure explicitly requests inspection.
- Does not verify the receiving thread's pitch, available engagement, blind tapped depth, screw strength or thread interference. The requested 1.5-diameter rule is a sizing rule, not a check of the receiving component.
- Never saves the assembly, closes user documents, quits Solid Edge or modifies existing hardware. Standard Parts may generate its selected library file through its normal workflow. `RememberedParts.xml` and `PopulateSHCS.log` are stored beside the executable; that directory must be writable to retain mappings/logs.

## Build and verification

Run `Build.cmd` with Solid Edge 2026 and .NET Framework installed. The executable is written directly into `Compiled Executables`. Interop types are embedded, so separate interop DLLs are not shipped. The installed Standard Parts provider must be configured for the optional native selector.

`tests/BuildTests.cmd`, followed by `tests/Tests.exe`, runs geometry-independent regression checks. `tests/Tests.exe --live` builds isolated scratch documents in `tests/artifacts`, exercises them in a running Solid Edge session and closes them without saving the resulting assembly. Before the live test, copy the configured Standard Parts **1/4 × 7/8 ANSI SHCS** to `tests/Sample-SHCS.par`; this proprietary library file is intentionally not redistributed. `tests/Tests.exe tests/Sample-SHCS.par` inspects its mounting geometry.

Validated on Solid Edge 2026: chart tie handling; exact and rounded-up lengths; native cylinder normals and shared shoulder recognition; real Standard Parts mounting geometry; direct, reversed and rotated nested occurrences; existing hardware; main-assembly insertion; duplicate prevention; mid-batch rollback; and stale-scan rejection. The native Standard Parts provider connection and actual Part Finder SHCS selection were inspected; the modal select/generate fallback still needs an end-to-end user trial.

Implementation follows the bundled SDK `SolidEdge.Part.FiniteExtrudedProtrusion` and `OleMessageFilter` examples: one STA thread, bounded busy retries, explicit short-lived geometry wrappers, no forced release of shared COM objects. Geometry/math tests are independent of COM.

Relevant Siemens API references: [AddWithMatrix](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~Occurrences~AddWithMatrix.html), [SubOccurrence.GetMatrix](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~SubOccurrence~GetMatrix.html), [Face.GetNormal](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeGeometry~Face~GetNormal.html), [AddGround](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~Relations3d~AddGround.html).
