# Solid Edge Convert 1.0

A small Windows Explorer converter based on the Siemens Batch sample. Version 1.0 supports a single `.par` file to STEP (`.stp`). Draft conversion and other formats can be added later.

## Install and use

1. Keep `Compiled Executables\SolidEdgeConvert.exe` in a permanent folder and double-click it.
2. Click **Install menu**. Installation applies only to your Windows account and does not require administrator rights.
3. Right-click a `.par` file and select **Convert > STEP (.stp)**. On Windows 11, first select **Show more options** (or press Shift+F10).
4. The result is saved alongside the original with the same base name: `Bracket.par` becomes `Bracket.stp`. Confirm before replacing an existing STEP file.

Solid Edge must be installed and licensed. The converter connects to the running application or starts it if needed. A progress window and completion/error message show the result. Solid Edge remains running afterwards, as it does with Batch.

Run the executable again and click **Remove menu** to uninstall. If you move the executable, run it from the new location and click **Install menu** again. No file associations or other programs' menus are changed. The menu applies only to `.par` and a single selected file.

Command-line alternatives (quote paths containing spaces):

```text
SolidEdgeConvert.exe --install
SolidEdgeConvert.exe --uninstall
SolidEdgeConvert.exe --step "C:\CAD\Bracket.par"
```

The two installation commands complete silently on success. Conversion remains interactive and prompts before replacement. Exit code is 0 for success or declined replacement and 1 for failure.

## Conversion behavior and references

The implementation follows `Batch_frm.vb` in the supplied `Documents\Codex\Batch` sample: remember `seApplicationGlobalSTEPAdapterKey`, enable it, call `Documents.Open`, `DoIdle`, `SaveAs` with a `.stp` extension, then `Close(false)` and `DoIdle`. The original translator setting is restored on both success and failure, including when its original value was false. The enum value 458 was verified against that sample's `Bin\Interop.SolidEdgeFramework.dll`.

The SDK's `SolidEdge.Automation` example supplies the STA/connection/message-filter pattern. COM references are individually released in reverse ownership order, without force-releasing shared wrappers or relying on garbage collection. Busy-call retries are bounded to 30 seconds; an export already executing inside Solid Edge is allowed to finish.

The tool reuses an open, saved part without closing it. An open part with unsaved changes is rejected so the user can save first. It never saves the source. It exports into a unique temporary `.stp` beside the source, checks for nonempty output, completes CAD cleanup, and only then moves/replaces the final output. Replacement uses Windows' file replacement operation; an unsupported or read-only destination produces an error and retains the old output. Temporary exports are removed on failure where permissions permit. Only one conversion from this tool can run at a time; avoid running other batch conversions against the same Solid Edge session concurrently.

The per-user cascading menu uses `HKCU\Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert`. The command launches the executable directly, with both paths quoted. References: [Microsoft cascading menus](https://learn.microsoft.com/en-us/windows/win32/shell/how-to-create-cascading-menus-with-the-extendedsubcommandskey-registry-entry) and [Windows 11 context menus](https://blogs.windows.com/blog/2021/07/19/extending-the-context-menu-and-share-dialog-in-windows-11/).

## Build and test

Run `Build.cmd` on 64-bit Windows with .NET Framework 4.x installed. It creates `Compiled Executables\SolidEdgeConvert.exe`. No package downloads, Visual Studio, or redistributed Siemens interop assemblies are required.

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1` to build the release and run regression tests. The harness uses simulated Solid Edge objects to verify translator restoration, cleanup after failures, replacement protection, source preservation, quoting, and document ownership. It does not establish translator fidelity in a live Solid Edge installation.

Optional integration checks:

- `LiveTest.ps1` requires an open, idle Solid Edge session and the Solid Edge 2026 part interop library used by the existing SketchToModels build. It creates its own cylinder under `tests\work`, exports it, checks STEP solid records and the source's SHA-256 hash, repeats with the part already open, and retains the fixture for inspection. It never modifies existing user documents.
- `TestMenu.ps1` runs the release executable's install/reinstall/uninstall commands, verifies the registry values, and leaves the menu installed at the current executable location.

Validation on September 28, 2026: release compilation, all 20 regression tests, live cylinder export/replacement/source preservation/translator restoration/open-document reuse, and menu installation/reinstallation/removal checks passed. Explorer's visual menu placement and geometry fidelity across production CAD parts still require the acceptance checks below.

Manual acceptance checks:

- Install the menu and convert a saved part; open the STEP in CAD and compare geometry and units.
- Repeat with an existing output; verify No retains it and Yes replaces it.
- Repeat while the source is already open; verify it remains open. Modify the part without saving; conversion should ask you to save first.
- Try a missing/corrupt part or an unwritable destination; confirm an error and no damaged prior output.
- Remove the menu and confirm the normal Solid Edge association remains intact.
