# Hammill Add Sketch v1.0

Standalone x64 .NET Framework macro extracted from BWH Add Sketch. Open an assembly, run **Add Sketch v1.0.exe**, choose an existing assembly reference plane and a quantity (1–100), then click **Add sketches**. The default is Right (yz) when present. The macro adds empty native assembly layouts/sketches. It creates no sketch geometry and does not save the assembly.

Plane names cannot be typed manually. The selected plane is revalidated before creation; each addition must increase the layout count. If Solid Edge refuses another layout on the plane, the macro reports the number actually created and the API status instead of reporting false success. A partial batch remains available for review/Undo. The prior DelayCompute setting is restored even after a failure. All COM automation runs on one STA with bounded retries and balanced releases.

Extract the ZIP with all four files together, or publish/install it through Macro Manager under **Solid Edge Macros**. Restart Solid Edge for Hammill Add-ins to discover its large ribbon button. No registration or original BWH DLL is required. Save the assembly yourself when satisfied.

Build with `Build.cmd`; output goes to **Compiled Executables**. All 13 `Test.cmd` regression checks passed, and the window was rendered and checked. A live Solid Edge acceptance test created two native sketches in a new, disposable assembly, saved that fixture, closed it and restored the prior active document. Common source files in `../Hammill Common` are compiled into the EXE, so no shared runtime DLL is needed.

API basis: supplied SDK automation/COM examples and Siemens [Layouts.Add](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~Layouts~Add.html). The omitted ReturnExisting argument matches the original BWH call; returned status and layout count are now checked.
