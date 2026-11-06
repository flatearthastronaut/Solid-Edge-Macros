# Hammill Occurrence Viewer v1.0

Standalone x64 .NET Framework replacement for BWH Occurrence Viewer. Open an assembly and run **Occurrence Viewer v1.0.exe**. Choose a filter, choose whether its value is Yes or No, optionally include subassemblies, then **Preview**, **Show matching**, or **Hide matching**. Visibility changes are not automatically saved.

The original filters are retained: All, Reference, Assembly reports/BOM, Drawing views, Physical properties, Higher level, Interference reports, User-defined quantity, and Selectable. User-defined quantity means Quantity differs from one; its No option now correctly selects Quantity equal to one. Filters only select occurrences; they do not change those property flags.

Nested traversal processes each assembly instance once, without the original repeated nested loops or invalid casts. It reads flags through ThisAsOccurrence but changes visibility through the actual SubOccurrence, preserving top-assembly instance context. Matching nested children does not automatically show a hidden parent. Unavailable properties/components are reported; other usable instances can still be processed. The original DelayCompute value is restored in a finally block, including on failures. Preview never changes visibility or compute settings.

Extract all four files from the ZIP into one folder, or publish/install through Macro Manager as **Solid Edge Macros**. Restart Solid Edge for the Hammill Add-ins ribbon button. No add-in registration or original BWH DLL is needed.

Build with `Build.cmd`, test with `Test.cmd`. Source in `../Hammill Common` is compiled into the EXE. Regression checks use fake CAD objects and isolated project fixtures; live Solid Edge 2026 verification remains required.

API basis: bundled SDK automation examples and Siemens [SubOccurrence members](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~SubOccurrence_members.html), including SubOccurrences, ThisAsOccurrence, Subassembly and Visible.
