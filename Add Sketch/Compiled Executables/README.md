# Add Sketch v1.2

Standalone x64 .NET Framework macro extracted from BWH Add Sketch. Open an assembly, run **Add Sketch v1.2.exe**, choose an existing assembly reference plane and a quantity (1–100), then click **Add sketches**. The default is Right (yz) when present. The macro adds empty native assembly layouts/sketches. It creates no sketch geometry and does not save the assembly.

Version 1.2 names the window **Add Sketch** and removes the oversized branded heading. The default content area is 390 × 204 pixels at 100% display scaling, with tighter field spacing and a smaller quantity box. The window remains resizable; hover over a status message to read its full text if a long diagnostic does not fit.

All 24 regression checks passed for v1.2. The compact window was also rendered and checked against the open assembly without adding or saving sketches; all four named planes loaded successfully.

Version 1.1 fixes an empty plane dropdown followed by `HRESULT E_FAIL`: some assemblies contain reference-plane entries with blank names or a failing Name getter. The macro now skips those individual entries, retains usable named planes before and after them, and displays the skipped count. Filtered choices retain their original native collection indexes. It does not delete, repair, or rename any reference planes. If the whole collection is unavailable, or no usable planes remain, the list is cleared and Add sketches is disabled; Reload planes can recover in the same window.

Plane names cannot be typed manually. The selected plane is revalidated before creation; each addition must increase the layout count. If Solid Edge refuses another layout on the plane, the macro reports the number actually created and the API status instead of reporting false success. A partial batch remains available for review/Undo. The prior DelayCompute setting is restored even after a failure. All COM automation runs on one STA with bounded retries and balanced releases.

Extract the ZIP with all four files together, or publish/install it through Macro Manager under **Solid Edge Macros**. Restart Solid Edge for Hammill Add-ins to discover its large ribbon button. No registration or original BWH DLL is required. Save the assembly yourself when satisfied.

Build with `Build.cmd`; output goes to **Compiled Executables**. Run `Test.cmd` for regression checks covering failed/blank/null entries, native-index retention, selected-plane revalidation, stale-list clearing, reload recovery, and the compact window layout. Version 1.1 was verified read-only against the reported assembly: Top (xy), Right (yz), Front (xz), and STOP HEIGHT loaded; 27 unnamed/unreadable entries were skipped. No sketch was created or saved in that assembly. A separate live acceptance test created two native sketches in a disposable assembly, saved that fixture, closed it and restored the prior active document. Version 1.2 retains that same sketch engine. Common source files in `../Hammill Common` are compiled into the EXE, so no shared runtime DLL is needed.

`tests/InspectPlanes.ps1` is a read-only diagnostic for the current assembly that checks the real dropdown and writes a preview under `tests/.work`. `tests/LiveSmoke.ps1` tests sketch creation in its own new assembly only. Updating this macro through Macro Manager retains the existing Add Sketch package family and ribbon target; no Hammill add-in update is required.

API basis: supplied SDK automation/COM examples and Siemens [Layouts.Add](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~Layouts~Add.html). The omitted ReturnExisting argument matches the original BWH call; returned status and layout count are now checked.
