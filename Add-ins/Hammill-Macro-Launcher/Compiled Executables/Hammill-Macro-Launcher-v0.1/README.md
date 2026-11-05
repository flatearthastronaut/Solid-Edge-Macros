# Hammill Macro Launcher v0.1

This add-in creates a **Hammill Macros** ribbon tab from macros installed by the user edition of the Solid Edge Macro & Add-in Manager. Solid Edge scans the installation records when the add-in connects at startup. Installing a new macro through the manager makes it eligible for a button at the next Solid Edge startup. Discovery never runs the macros.

This is the first compiled launcher module for Hammill Add-ins. The five retained legacy utilities have not yet been migrated. Its separate COM identity preserves the original BWH installation.

## Folder and discovery

- Read `<local>` from `%LOCALAPPDATA%\Coop\SEMacroManager\settings.xml` on every startup.
- If the user manager has no saved local preference, use the user's Windows Documents folder plus `Add-ins`, matching the manager's default. Documents may be redirected by Windows.
- Do not use the admin edition's settings, shared publishing folder, or source/build directory.
- Read `.se-manager\*.xml` installation manifests. Packages with `stableCopies="1"` were installed as **Solid Edge Macros** by manager v1.11 or later.
- Choose the unversioned main executable matching the package family. Numbered copies, helper EXEs, add-ins, Windows programs and unmanaged executables do not become buttons.
- If there are multiple possible main executables, report the package for explicit configuration. Repair missing stable copies through Macro Manager.
- Use a matching installed PNG, ICO or BMP when supplied. Otherwise use the executable's associated icon, or the Windows application icon. The launcher creates small and large native bitmap resources in its private cache.

Older installed packages without stable-copy metadata need an update or repair through the current user manager. Copying an arbitrary EXE into this folder does not by itself register it as a macro. This keeps utilities and installers out of the ribbon.

## Build and distribution

Run `Build.cmd` on 64-bit Windows with the .NET Framework compiler. The output is an x64 CLR 4 class library and diagnostic executable in **Compiled Executables**. The build also produces `Hammill-Macro-Launcher-v0.1.zip`, suitable for the existing Macro Manager's add-in installation workflow.

The framework interop in `lib` is the supplied legacy package's `Interop.SolidEdgeFramework.dll`. The launcher uses `ISolidEdgeAddIn`, `ISEAddInEx2` and paired command events, checked against `SDK_2026_2510_English/SDK/Advanced/samples/Addins/VC/SEAddin.cpp`, the VB.NET add-in example, and `include/secatids.h`. It does not load or invoke the legacy `SEAddIn.dll`.

To install manually, extract the ZIP into a permanent local folder, close Solid Edge, and run its `Register.cmd` as administrator. The Macro Manager can also install the ZIP and run its registration script. Launch Solid Edge and check **Settings > Add-Ins** for **Hammill Macro Launcher**. Registering the add-in does not install any of the macros themselves.

To remove this launcher, close Solid Edge and run its `UNRegister.cmd` as administrator. Both scripts check for running Solid Edge processes. They use the launcher's own COM identity and do not unregister BWH.

## Behavior and customization

The launcher creates buttons in Part, Assembly, Draft and Sheet Metal, including the corresponding synchronous environments. By default every detected macro is offered in those document environments. A **Macro folder** command shows the source folder and startup scan notices. No macros are launched automatically. Each clicked EXE runs with its own directory as its working directory.

Optional `HammillMacros.xml` belongs at the root of the manager's local install folder. Example:

```xml
<macros>
  <macro package="dualdimensiontoggle"
         name="Dual Dimensions"
         group="Drawing"
         environments="draft" />
  <macro package="sketchtomodels"
         group="Modeling"
         environments="part,assembly" />
  <macro package="somepackage" enabled="false" />
</macros>
```

The package key is the filename of its `.se-manager` XML manifest without `.xml`. Supported environments are `part`, `assembly`, `draft`, and `sheetmetal`. Optional `path` selects a particular stable EXE already listed in that package's manifest; it is relative to the install folder. Optional `icon` supplies a relative image path. Invalid or ambiguous entries are reported and skipped. The add-in package does not overwrite this customization file.

Restart Solid Edge after changing the installed macro list or configuration. Ordinary macro updates that retain the same stable filename keep the existing button. The launcher persists its original command IDs and advances Solid Edge's `GuiVersion` only when the catalog, labels, groups, environments or companion icons change.

**Solid Edge rebuilds this add-in's cached GUI when `GuiVersion` changes. Customizations or shortcuts attached to its buttons may need to be reapplied after a catalog/layout change.** Existing unrelated ribbons are not intentionally edited. Live SE testing is required to verify the host's exact customization behavior.

State, generated resource files and a bounded diagnostic log are stored under `%LOCALAPPDATA%\Hammill\MacroLauncher`. A retained resource cache lets existing sessions continue using their current icons. The launcher does not monitor folders continuously or refresh ribbon controls during an active CAD command.

## Validation status

`Test.ps1` builds the launcher and runs 30 automated checks using project-local fixtures. These cover manager settings, stable target selection, exclusion of helper/versioned executables, nested packages, configuration, missing/ambiguous targets, path confinement, XML parsing, stable command IDs, added/removed macros, icon changes, concurrent startups, changed local folders, and loading generated small/large Win32 bitmap resources.

`Hammill Macro Catalog Check.exe` prints the folder, discovered macros and notices without starting Solid Edge or executing macros. It accepts `--settings PATH` and `--documents PATH` for isolated diagnostics. Without arguments it reads the user manager's actual settings.

Compilation and offline regression checks have passed. The add-in has **not yet been registered or tested inside SE 2026**. Live acceptance must verify: startup/load and clean shutdown; the tab and icons in each supported document environment; launching a known test macro; installing/removing a macro and restarting; and the effect of a catalog change on customized buttons/shortcuts.
