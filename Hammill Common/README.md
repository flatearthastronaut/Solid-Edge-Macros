# Hammill shared build sources

These source files are compiled into Add Sketch, Occurrence Viewer, Batch Approval Export, and the combined Hammill add-in. They are not a separately distributed DLL or installed add-in. O-Ring Finder remains its already-built independent macro.

- `ComSupport.cs`: deterministic, per-acquisition COM cleanup and the standalone macros' bounded STA message filter, following the bundled SDK automation examples.
- `MacroHost.cs`: attach to the user's running Solid Edge session, validate the active document type, and show a branded standalone window. It never closes the user's documents or Solid Edge.
- `Build-Macro.ps1` and `Build-Icon.ps1`: compile each named x64 macro and create its Hammill-green icon and Macro Manager ZIP in that project's Compiled Executables folder.
- `Test-Macro.ps1`: compile and execute each macro's isolated regression tests.
- `PackageCheck.ps1`: install the four macro ZIPs in a project-local fixture with the actual Macro Manager v1.14; verify stable EXEs/icons and the external O-ring table; confirm the latest Hammill package is one add-in and the v0.3 launcher discovers all four macros. It does not register the add-in or modify a user's installed macros.

Current verification: 54 combined add-in checks, 13 Add Sketch checks, 17 Occurrence Viewer checks, 17 Batch Approval Export checks, and the full package check passed. All three new macro windows were rendered and inspected. Add Sketch also passed live native sketch creation in a disposable assembly. The v0.3 icon resource preview passed; the updated add-in and other new workflows still need live Solid Edge acceptance on representative files.
