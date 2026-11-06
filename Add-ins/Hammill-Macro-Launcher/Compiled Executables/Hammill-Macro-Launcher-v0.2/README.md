# Hammill Add-ins v0.2

This add-in creates a **Hammill Macros** ribbon tab from macros installed by the user edition of the Solid Edge Macro & Add-in Manager. Solid Edge scans the installation records when the add-in connects at startup. Installing a new macro through the manager makes it eligible for a button at the next Solid Edge startup. Discovery never runs the macros.

This single add-in combines the macro launcher and automatic **Component Display**, migrated from BWH. **O-Ring Finder**, **Add Sketch**, **Occurrence Viewer**, and **Batch Approval Export** are standalone macros. They do not require the BWH DLL. The existing Hammill launcher COM identity and `Hammill-Macro-Launcher` package/DLL filenames are retained so Macro Manager upgrades the existing installation rather than treating the combined add-in as a second product. The displayed add-in name is now **Hammill Add-ins**.

Version 0.2 requests **large macro buttons with captions below the icons**, using the existing 32 × 32 resources and the SDK's `seButtonIconAndCaptionBelow` style. Upgrading refreshes the launcher's cached GUI once even if the installed macro list has not changed; command IDs are preserved. The Macro folder command and artwork-failure fallback remain caption-only.

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

Run `Build.cmd` on 64-bit Windows with the .NET Framework compiler. The output is an x64 CLR 4 class library and diagnostic executable in **Compiled Executables**. The build also produces `Hammill-Macro-Launcher-v0.2.zip`, suitable for the existing Macro Manager's add-in installation workflow.

The framework interop in `lib` is the supplied legacy package's `Interop.SolidEdgeFramework.dll`. The add-in uses `ISolidEdgeAddIn`, `ISEAddInEx2`, `ISEApplicationEvents_Event`, and paired event subscriptions, checked against `SDK_2026_2510_English/SDK/Advanced/samples/Addins/VC/SEAddin.cpp`, the VB.NET add-in example, the ApplicationEvents sample, and `include/secatids.h`. Shared COM cleanup source in `../../Hammill Common` is compiled into the DLL. It does not load or invoke the legacy `SEAddIn.dll`.

To install manually, extract the ZIP into a permanent local folder, close Solid Edge, and run its `Register.cmd` as administrator. The Macro Manager can also install the ZIP and run its registration script. Launch Solid Edge and check **Settings > Add-Ins** for **Hammill Add-ins**. Registering the add-in does not install any of the macros themselves.

For a manual upgrade from v0.1, close Solid Edge, run the old launcher's `UNRegister.cmd` as administrator, then run the new v0.2 folder's `Register.cmd` as administrator. Restart Solid Edge to apply the large-button layout. If installed through Macro Manager, publish v0.2 and use **Install / update selected**; the manager runs the old and new registration scripts.

For the BWH cutover, install the four standalone macro packages as **Solid Edge Macros** and the v0.2 ZIP as **Solid Edge Add-ins**. With Solid Edge closed, run **BWH's own UNRegister.cmd**, located beside its original `SEAddIn.dll`, as administrator. Keep that old package as a recovery copy. Restart Solid Edge with only **Hammill Add-ins** enabled; do not run both Component Display handlers together. The new registration script deliberately does not unregister BWH automatically. Check the four macro workflows and Update Views on a copy of a representative drawing before distributing company-wide.

To remove this launcher, close Solid Edge and run its `UNRegister.cmd` as administrator. Both scripts check for running Solid Edge processes. They use the launcher's own COM identity and do not unregister BWH.

## Automatic Component Display

There is **no Component Display ribbon button or icon**. The add-in runs the migrated behavior before the existing **Update Views** command (`DetailDrawingViewsUpdateViews`, 57083), and only when a Draft is active. It traverses drawing-view model members, including nested assemblies and sheet-metal parts:

- `UC_` and `REF_` prefixes set reference display and Phantom visible/hidden edge styles.
- `NORM_` and `UNDO_` prefixes restore normal display with Visible/Hidden edge styles.
- A matching assembly prefix applies to the whole branch. Part prefixes apply to members containing a Design Body.
- Body names whose segment after the first dot starts `THRD_` use Show Part with Hidden edges. `TB_` uses Show Part with Phantom edges and Show Hidden Edges enabled.

Unmarked components retain their presentation. Unchanged values are not rewritten. Component-level errors are logged without interrupting the host command; recursion is bounded and reentrant command notifications are ignored. The add-in never saves the draft on its own. Application-event subscriptions are removed on disconnect.

API reference: Siemens [DetailCommandConstants](https://support.industrysoftware.automation.siemens.com/trainings/se/107/api/SolidEdgeConstants~DetailCommandConstants.html); behavior recovered from the supplied `SEAddIn.RefStyle` and `SEAddIn.Addin` types.

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

`Test.ps1` builds the combined add-in and runs 46 automated checks using project-local fixtures. These cover Component Display prefixes, body styles, nested branches, sheet metal, command filtering and idempotence, plus manager settings, stable target selection, exclusion of helper/versioned executables, nested packages, configuration, missing/ambiguous targets, path confinement, XML parsing, stable command IDs, added/removed macros, icon changes, concurrent startups, changed local folders, and loading generated small/large Win32 bitmap resources. Tests capture the actual registration call to verify large-button style and bitmap IDs, verify the native large bitmap is 32 × 32, and exercise a v0.1 saved-GUI upgrade with unchanged macro IDs and a single GUI refresh. `../../Hammill Common/PackageCheck.ps1` verifies one unambiguous add-in package, installation of all four macros, their stable files, and launcher discovery through the actual manager binaries in isolated project folders.

`Hammill Macro Catalog Check.exe` prints the folder, discovered macros and notices without starting Solid Edge or executing macros. It accepts `--settings PATH` and `--documents PATH` for isolated diagnostics. Without arguments it reads the user manager's actual settings.

Compilation, offline regression checks, package installation/discovery checks, and standalone macro window renders passed. Add Sketch additionally created two native sketches in a disposable Solid Edge assembly. The **combined v0.2 add-in has not yet been registered or visually checked inside SE 2026**. Live acceptance still must verify: Component Display before Update Views; startup/load and clean shutdown; large buttons with captions below in each supported document environment; the other standalone macro workflows and real exports; installing/removing a macro and restarting; and the effect of a catalog change on customized buttons/shortcuts.
