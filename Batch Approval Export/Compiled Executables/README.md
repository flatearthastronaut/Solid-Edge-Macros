# Hammill Batch Approval Export v1.0

Standalone x64 .NET Framework replacement for the BWH approval/export window. Open a saved draft in Solid Edge and run **Batch Approval Export v1.0.exe**. Choose a linked assembly when 3D output is wanted, the filename date, destination folder and formats. Review **Preview files**, then choose **Export selected**.

The four original formats are retained: draft DWG, draft 2D PDF, linked assembly STEP and assembly 3D PDF. If there is no linked assembly, 2D export remains available. Selecting an assembly explicitly handles drafts with multiple assembly links. “Approval” is the inherited workflow name: this macro does not mark files approved or change approval/status properties.

By default each output goes beside its source document, named `SourceName yyyyMMdd.ext`. A selected output folder applies to all exports. The 3D PDF is named `AssemblyName yyyyMMdd 3D.pdf` to avoid overwriting a same-named 2D PDF. Existing files are skipped, including files created by another process after preview. Choose another folder/date or remove an old output yourself to replace it.

The exporter uses disposable native copies created with SaveCopyAs, then exports those copies. It does not save, rename or close the user's original draft/assembly. Native snapshots include current document state; the macro reactivates the source draft when finished. Outputs are staged and checked for nonzero length before being moved into place. Each job reports success, skip or failure; other jobs continue. Temporary copies are closed without saving; if closing/cleanup fails, the retained location is reported for review. CAD translator defaults such as PDF sheets/colors and DWG mappings remain the workstation's settings.

Extract all four files from the ZIP together, or publish/install through Macro Manager under **Solid Edge Macros**. Restart Solid Edge for the Hammill Add-ins ribbon button. No add-in registration or original BWH DLL is required.

Build with `Build.cmd`, test with `Test.cmd`. Source in `../Hammill Common` is compiled into the EXE. Offline regression checks validate output planning, collisions, skips, staging, cleanup and error paths with fake exporters; real translator output and linked-model resolution require live Solid Edge 2026 acceptance.

API basis: bundled SDK automation examples and Siemens [draft SaveCopyAs](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeDraft~DraftDocument~SaveCopyAs.html), [assembly SaveCopyAs](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~AssemblyDocument~SaveCopyAs.html) and [assembly SaveAs](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeAssembly~AssemblyDocument~SaveAs.html). FileFormat=True selects 3D PDF.
