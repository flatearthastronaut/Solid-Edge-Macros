# Solid Edge Convert 1.5

A small Windows Explorer converter based on the Siemens Batch sample. Supports parts (`.par`) to STEP (`.stp`), drafts (`.dft`) to PDF, and STEP files (`.stp` or `.step`) to Solid Edge parts (`.par`) using `normal.par`. Select one or more files supported by the same conversion; `.stp` and `.step` may be selected together.

Parasolid files (`.x_t` text or `.x_b` binary) have two choices: **Convert > Solid Edge Part (.par)** using `normal.par`, and **Convert > Solid Edge Assembly (.asm)** using `normal.asm`. Both extensions can be selected together. Each selected file produces its own part or assembly; the selection is not combined into one assembly.

## Install and use

1. Keep `Compiled Executables\SolidEdgeConvert.exe` in a permanent folder and double-click it.
2. Click **Install menus**. Installation applies only to your Windows account and does not require administrator rights. Run this again when upgrading an older installation to enable multiple selections.
3. Select `.par` files and choose **Convert > STEP (.stp)**, `.dft` files and choose **Convert > PDF (.pdf)** or **PDF with Date**, or STEP files and choose **Convert > Solid Edge Part (.par)**. On Windows 11, first select **Show more options** (or press Shift+F10).
4. Each result is saved alongside its original with the same base name: `Bracket.par` becomes `Bracket.stp`; `Drawing.dft` becomes `Drawing.pdf`; `Imported.step` becomes `Imported.par`.
5. If outputs already exist, choose **Yes** to replace existing outputs, **No** to skip them, or **Cancel** to cancel the batch. Files convert sequentially in one window; the final list identifies every created, skipped, and failed file. A failed file does not stop the remaining files.

**PDF with Date** uses the same PDF export and adds one space plus the local date before `.pdf`: `Drawing.dft` becomes `Drawing 20260928.pdf` on September 28, 2026. The date uses Gregorian `YYYYMMDD` regardless of regional settings and is captured once before the overwrite prompt, so every file in a batch uses the same date even across midnight. A same-day dated output uses the normal replace/skip prompt; the undated PDF is separate.

Solid Edge must be installed and licensed. The converter connects to the running application or starts it if needed. Solid Edge remains running afterwards, as it does with Batch.

Run the executable again and click **Remove menus** to uninstall all six file-extension menus and their six COM registrations. If you move the executable, run it from the new location and click **Install menus** again. No file associations or other programs' menus are changed. Run **Install menus** after upgrading to add the Parasolid options.

Command-line alternatives (quote paths containing spaces):

```text
SolidEdgeConvert.exe --install
SolidEdgeConvert.exe --uninstall
SolidEdgeConvert.exe --step "C:\CAD\Bracket.par"
SolidEdgeConvert.exe --pdf "C:\CAD\Drawing.dft"
SolidEdgeConvert.exe --step "C:\CAD\Part1.par" "C:\CAD\Part2.par"
SolidEdgeConvert.exe --pdf "C:\CAD\Drawing1.dft" "C:\CAD\Drawing2.dft"
SolidEdgeConvert.exe --pdf-date "C:\CAD\Drawing1.dft" "C:\CAD\Drawing2.dft"
SolidEdgeConvert.exe --part "C:\CAD\Imported1.stp" "C:\CAD\Imported2.step"
SolidEdgeConvert.exe --parasolid-part "C:\CAD\Imported1.x_t" "C:\CAD\Imported2.x_b"
SolidEdgeConvert.exe --parasolid-assembly "C:\CAD\Imported1.x_t" "C:\CAD\Imported2.x_b"
```

The two installation commands complete silently on success. Conversion remains interactive and prompts before replacement. Direct command-line conversion returns 0 when no files failed (including skipped/cancelled work), and 1 when a file failed. Explorer launches a reusable local server; consult the results list for each batch's outcome.

## Conversion behavior and references

The implementation follows `Batch_frm.vb` in the supplied `Documents\Codex\Batch` sample: remember `seApplicationGlobalSTEPAdapterKey`, enable it, call `Documents.Open`, `DoIdle`, `SaveAs` with a `.stp` extension, then `Close(false)` and `DoIdle`. The original translator setting is restored on both success and failure, including when its original value was false. The enum value 458 was verified against that sample's `Bin\Interop.SolidEdgeFramework.dll`.

STEP import follows Batch's `Documents.OpenWithTemplate(source, "normal.par")` path, then saves as `.par`. Solid Edge resolves `normal.par` from its configured templates; it must be available on each machine. The STEP adapter is enabled and restored around the import. Only a newly imported part is saved and closed; a returned document that was already open is rejected and left untouched. If both `Name.stp` and `Name.step` are selected, the second is reported as a filename conflict because both would produce `Name.par`; rename one or convert it separately.

Draft conversion follows Batch's ordinary **Adobe (*.pdf)** path: `SaveAs` with a `.pdf` extension, without the 3D PDF argument. It uses Solid Edge's PDF export defaults and does not read or change the STEP translator setting. The SDK's `SolidEdge.Draft.Sheets` example supplies the draft sheet access pattern used by the live PDF test.

Parasolid import follows Batch's `OpenWithTemplate` and `SaveAs` sequence, using `normal.par` or `normal.asm` according to the selected output. It checks the returned native document type and leaves the STEP setting untouched. Solid Edge must be able to resolve both templates. Text/binary files with the same base name collide for a given output format, so the second is reported as a filename conflict.

An assembly is more than a single file. `Example.x_t` produces `Example.asm` beside its source and a uniquely named `Example Components <id>` folder containing its referenced parts and any translator auxiliary files. Keep that folder with the assembly when moving or sharing it. Import uses a temporary copy of the input inside this folder to isolate generated components from existing files; the input copy is removed afterwards. Replacement creates a fresh component folder and preserves older folders, which may still be referenced elsewhere. Failed imports may retain generated components for recovery. Cleanup never recursively deletes component folders.

The SDK's `SolidEdge.Automation` example supplies the STA/connection/message-filter pattern. COM references are individually released in reverse ownership order, without force-releasing shared wrappers or relying on garbage collection. Busy-call retries are bounded to 30 seconds; an export already executing inside Solid Edge is allowed to finish.

The tool reuses an open, saved part or draft without closing it. An open document with unsaved changes is rejected so the user can save first. It never saves the source. It exports into a unique temporary `.stp`, `.pdf`, or `.par` beside the source, checks for nonempty output, completes CAD cleanup, and only then moves/replaces the final output. Replacement uses Windows' file replacement operation; an unsupported or read-only destination produces an error and retains the old output. Temporary exports are removed on failure where permissions permit. Only one conversion from this tool can run at a time; avoid running other batch conversions against the same Solid Edge session concurrently.

The per-user cascading menus use `HKCU\Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert` and the equivalent `.dft`, `.stp`, `.step`, `.x_t`, and `.x_b` keys. Both levels specify `MultiSelectModel=Player`. Each subcommand's `DelegateExecute` activates a per-user local COM server in the same executable. `IObjectWithSelection` receives the whole selection as an `IShellItemArray`; `IExecuteCommand` queues a single batch. This avoids launching a process for every filename and avoids command-line selection truncation. The converter does not load a managed DLL into Explorer. It retires its server after 60 seconds without requests or an open batch/results window. Wait for that idle exit before rebuilding/replacing the executable if Windows reports it is in use.

Server class IDs under `HKCU\Software\Classes\CLSID`:
- STEP: `{B84A6BE1-A4D2-4CD2-A1AE-60EAA476AD11}`
- PDF: `{AF39D53D-3C70-4055-8197-442F4C5180B2}`
- PDF with Date: `{D8F750C4-A88D-4F7A-BD12-590D6813B742}`
- STEP to Part (both extensions): `{46FDDBA1-7601-445B-A3A6-7F7624B196C2}`
- Parasolid to Part: `{263031DD-6A5B-4776-B4BC-39EB97D16A95}`
- Parasolid to Assembly: `{826FDBF0-726C-4EE8-B8B2-AE63E63ACAF9}`

The `LocalServer32` command is the quoted executable path followed by `--shell-server`. Registration and removal affect only those six classes and the converter's six menu keys. This internal server switch is not a user-facing conversion command.

References: [Microsoft ExecuteCommand sample](https://learn.microsoft.com/en-us/windows/win32/shell/samples-executecommandverb), [verb selection models](https://learn.microsoft.com/en-us/windows/win32/shell/how-to-employ-the-verb-selection-model), [cascading menus](https://learn.microsoft.com/en-us/windows/win32/shell/how-to-create-cascading-menus-with-the-extendedsubcommandskey-registry-entry), and [Windows 11 context menus](https://blogs.windows.com/blog/2021/07/19/extending-the-context-menu-and-share-dialog-in-windows-11/).

## Build and test

Run `Build.cmd` on 64-bit Windows with .NET Framework 4.x installed. It creates `Compiled Executables\SolidEdgeConvert.exe`. No package downloads, Visual Studio, or redistributed Siemens interop assemblies are required.

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1` to build the release and run regression tests. The harness uses simulated Solid Edge objects to verify translator restoration, cleanup after failures, replacement protection, source preservation, quoting, and document ownership. It does not establish translator fidelity in a live Solid Edge installation.

Optional integration checks:

- `LiveTest.ps1` requires an open, idle Solid Edge session and the Solid Edge 2026 part interop library used by the existing SketchToModels build. It creates its own cylinder under `tests\work`, exports it, checks STEP solid records and the source's SHA-256 hash, repeats with the part already open, and retains the fixture for inspection. It never modifies existing user documents.
- `LivePdfTest.ps1` requires an open, idle Solid Edge session. It creates a draft containing a rectangle and circle, exports a PDF, checks its header/end marker and the source's SHA-256 hash, and repeats with the draft already open and output already present. It also verifies the STEP setting is unchanged and retains the fixture under `tests\work` for inspection.
- `TestMenu.ps1` runs the release executable's install/reinstall/uninstall commands, verifies all six extensions' registry values, and leaves the menus installed at the current executable location.
- `ShellTest.ps1` requires the generated fixtures from both live tests and installed menus. It invokes the actual Windows `IContextMenu` on three files for each option (STEP, PDF, PDF with Date, STEP to Part, Parasolid to Part, Parasolid to Assembly), checks all eighteen exports and source hashes, and closes only the converter's completed three-file results windows. The STEP selection mixes `.stp` and `.step`. Parasolid tests generate a two-component assembly and export true text/binary fixtures, then mix `.x_t` and `.x_b` in each selection. Native outputs are reopened to check part models or assembly occurrences, component file existence, and component geometry. This tests selection delivery, dated filenames, and COM server activation as well as CAD conversion.

Validation: all 58 regression tests (including dated names, locale independence, fixed batch dates, per-file failure continuation, duplicate selection removal, skip/replace policies, and native Shell selection handling), real three-file context-menu conversions for all six conversions, source hashes, and menu/server installation/reinstallation/removal checks passed. The previous single-file live tests also covered replacement, open-document reuse, and translator restoration. Production drawing layout, sheet coverage, and CAD geometry fidelity still require the acceptance checks below.

Manual acceptance checks:

- Install the menus and convert several saved parts together; open the STEP files in CAD and compare geometry and units.
- Convert representative `.x_t` and `.x_b` files to both native types; verify geometry and units. Reopen an imported assembly and check its components. Keep its Components folder when moving/sharing it.
- Convert a saved draft; open the PDF and check drawing appearance and sheet coverage, including a multi-sheet production draft.
- Repeat with existing outputs; verify No skips them, Yes replaces them, and Cancel cancels the batch.
- Repeat while the source is already open; verify it remains open. Modify the part or draft without saving; conversion should ask you to save first.
- Try a missing/corrupt part or an unwritable destination; confirm an error and no damaged prior output.
- Remove the menu and confirm the normal Solid Edge association remains intact.
