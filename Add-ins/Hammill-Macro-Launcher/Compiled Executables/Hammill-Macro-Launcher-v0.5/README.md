# Hammill Add-ins v0.5

This add-in creates a **Hammill Macros** ribbon tab from macros installed by the user edition of the Solid Edge Macro & Add-in Manager. Solid Edge scans the installation records when the add-in connects at startup. Installing a new macro through the manager makes it eligible for a button at the next Solid Edge startup. Discovery never runs the macros.

This single add-in combines the macro launcher, automatic **Component Display**, and the built-in **Cycle Dimension Style** and **Cycle Limit Tolerance** commands. **O-Ring Finder**, **Add Sketch**, **Occurrence Viewer**, and **Batch Approval Export** are standalone macros. They do not require the BWH DLL. The existing Hammill launcher COM identity and `Hammill-Macro-Launcher` package/DLL filenames are retained so Macro Manager upgrades the existing installation rather than treating the combined add-in as a second product. The displayed add-in name is **Hammill Add-ins**.

Version 0.5 adds resident limit-tolerance cycling alongside the existing configurable dimension-style command. It retains v0.3's native alpha **PNG resources**, avoiding purple fringes. Following the 2026 SDK, large buttons use **32 × 32 artwork at normal DPI and 40 × 40 artwork at higher DPI**, with `seButtonIconAndCaptionBelow`. Upgrading refreshes the cached GUI once to add the new Drafting command; existing macro command IDs are preserved. The Macro folder command and artwork-failure fallback remain caption-only.

## Folder and discovery

- Read `<local>` from `%LOCALAPPDATA%\Coop\SEMacroManager\settings.xml` on every startup.
- If the user manager has no saved local preference, use the user's Windows Documents folder plus `Add-ins`, matching the manager's default. Documents may be redirected by Windows.
- Do not use the admin edition's settings, shared publishing folder, or source/build directory.
- Read `.se-manager\*.xml` installation manifests. Packages with `stableCopies="1"` were installed as **Solid Edge Macros** by manager v1.11 or later.
- Choose the unversioned main executable matching the package family. Numbered copies, helper EXEs, add-ins, Windows programs and unmanaged executables do not become buttons.
- If there are multiple possible main executables, report the package for explicit configuration. Repair missing stable copies through Macro Manager.
- Use a matching installed PNG, ICO or BMP when supplied. Otherwise use the executable's associated icon, or the Windows application icon. The launcher creates native alpha PNG resources in its private cache. Existing opaque artwork stays opaque; the launcher does not guess a background color to remove.

Older installed packages without stable-copy metadata need an update or repair through the current user manager. Copying an arbitrary EXE into this folder does not by itself register it as a macro. This keeps utilities and installers out of the ribbon.

## Build and distribution

Run `Build.cmd` on 64-bit Windows with the .NET Framework compiler. The output is an x64 CLR 4 class library and diagnostic executable in **Compiled Executables**. The build also produces `Hammill-Macro-Launcher-v0.5.zip`, suitable for the existing Macro Manager's add-in installation workflow.

The framework interop in `lib` is the supplied legacy package's `Interop.SolidEdgeFramework.dll`. The add-in uses `ISolidEdgeAddIn`, `ISEAddInEx2`, `ISEApplicationEvents_Event`, and paired event subscriptions, checked against `SDK_2026_2510_English/SDK/Advanced/samples/Addins/VC/SEAddin.cpp`, the VB.NET add-in example, the ApplicationEvents sample, and `include/secatids.h`. Shared COM cleanup source in `../../Hammill Common` is compiled into the DLL. It does not load or invoke the legacy `SEAddIn.dll`.

To install manually, extract the ZIP into a permanent local folder, close Solid Edge, and run its `Register.cmd` as administrator. The Macro Manager can also install the ZIP and run its registration script. Launch Solid Edge and check **Settings > Add-Ins** for **Hammill Add-ins**. Registering the add-in does not install any of the macros themselves.

For a manual upgrade, close Solid Edge, run the old launcher's `UNRegister.cmd` as administrator, then run the new v0.5 folder's `Register.cmd` as administrator. Restart Solid Edge to refresh the ribbon. If installed through Macro Manager, publish v0.5 and use **Install / update selected**; the manager runs the old and new registration scripts. No manual icon-cache deletion or macro rebuilds are required.

For the BWH cutover, install the four standalone macro packages as **Solid Edge Macros** and the v0.5 ZIP as **Solid Edge Add-ins**. With Solid Edge closed, run **BWH's own UNRegister.cmd**, located beside its original `SEAddIn.dll`, as administrator. Keep that old package as a recovery copy. Restart Solid Edge with only **Hammill Add-ins** enabled; do not run both Component Display handlers together. The new registration script deliberately does not unregister BWH automatically. Check the four macro workflows and Update Views on a copy of a representative drawing before distributing company-wide.

To remove this launcher, close Solid Edge and run its `UNRegister.cmd` as administrator. Both scripts check for running Solid Edge processes. They use the launcher's own COM identity and do not unregister BWH.

## Cycle Dimension Style

In a draft, select **one existing dimension**, then click **Hammill Macros > Drafting > Cycle Dimension Style**. Each click advances to the next style in that dimension's configured family and wraps after the last entry. The same dimension stays selected. The command runs directly inside the loaded add-in: no separate EXE, reconnection to Solid Edge, or success dialog. Repeated clicks use a cached numeric style index and parsed configuration; they do not scan all styles each time.

Edit **Hammill.DimensionStyles.txt in the Macro Manager's local installation folder** (normally Documents\Add-ins). The **Macro folder** button shows the exact path. The add-in creates this file from the packaged DimensionStyles.txt on first startup and never replaces an existing user file during an upgrade. Do not edit the packaged starter file after installation; edit the working Hammill.DimensionStyles.txt copy.

The file has four ordered lists: `[Inch]`, `[Inch-Metric]`, `[Vertical Inch]`, and `[Vertical Inch-Metric]`. Put exact style names from your draft on separate lines, in the order you want. Save the file; the next click loads the new order without rebuilding or restarting Solid Edge. Names are case-insensitive and leading/trailing spaces are ignored. A style can belong to only one list. Lines beginning with `#` or `;` are comments. For example:

```ini
[Inch]
1 2 place
1 3 place
1 4 place
1 5 place
```

The starter file includes the existing numbered inch/dual names and `Vert- N PLC` / `Vert - NPLC m[i]` vertical names recognized by Dual Dimension Toggle. Replace them if your draft uses a different naming convention; the file includes examples of numbered `(vert)` names. The selected dimension's **current style name** chooses the family, not its geometric direction. A missing next style, duplicate configured name, unknown current style, or change of units/text orientation stops the click with an explanation. Styles are not created or silently skipped.

Only the selected dimension's private style is changed. Engineering text, explicit tolerance values, display mode, measured size and track/break placement are preserved. The new named style supplies decimal precision and other style presentation; local style overrides may be replaced. Shared definitions and the default style are not edited. Nothing is saved automatically. Angular dimensions, callouts and other selected object types are left unchanged. Native style lookup follows the supplied SDK's DimensionStyles.Item numeric index; it validates cached names after document/style changes without retaining COM references between clicks.

## Cycle Limit Tolerance

Select one existing draft dimension, then click **Hammill Macros > Drafting > Cycle Limit Tolerance**. The large Hammill-green cycle/plus-minus icon applies native upper/lower **Limits** display. Each click advances through these inch tolerances and wraps:

| Tolerance | Inch decimal places | Metric decimal places |
| --- | --- | --- |
| +/-.0005 inch | 4 | 3 |
| +/-.001 inch | 3 | 2 |
| +/-.002 inch | 3 | 2 |

For a nominal 1-inch measurement, the displayed inch limits cycle through **1.0005 / 0.9995**, **1.001 / 0.999**, and **1.002 / 0.998**. An existing matching Limits pair advances to the next preset; a different tolerance or display mode starts at +/-.0005. These three presets are built in and do not use the dimension-style text file.

The same dimension stays selected. The command runs in the loaded add-in without launching an EXE or showing a success dialog. It preserves the measured value, geometry links, named style, inch/dual unit order, horizontal/vertical text orientation, engineering text and manual placement. Only the selected dimension's local tolerance and precision settings change; shared style definitions stay unchanged. Dual units use the same physical tolerance, converted by Solid Edge, with exactly one fewer metric decimal place. Inch-only dimensions retain unused secondary settings. No document is saved automatically.

Angular dimensions, non-dimension selections, overridden measurement text, fractional inch styles, and metric-only styles are rejected with an explanation. A failed change attempts to restore both original limits and precisions and reports any recovery failure. The implementation sets explicit inch values in native UnitTolerance mode before selecting Limits: assigning tolerance strings directly in Limits mode leaves Solid Edge's numeric limits stale. Native rendered-text tests cover this distinction.

The two drafting commands can be used together. Cycle Dimension Style applies the chosen named style's precision; Cycle Limit Tolerance applies the exact precision pair in the table above.

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

`Test.ps1` builds the combined add-in and runs 188 automated checks using project-local fixtures. These cover style-list ordering and family isolation, wraparound, live configuration edits, selection scope, units/orientation guards, protected dimension fields, failed assignment recovery, cached indexes, the actual add-in command handler and large-button registration. They also retain the earlier Component Display, macro discovery, path/XML safety, stable command IDs, concurrent GUI updates and alpha PNG checks. Saved-GUI upgrades from v0.1, v0.2, v0.3 and v0.4 preserve macro IDs. The PNG resource convention and DPI sizes follow `SDK_2026_2510_English/SDK/Advanced/samples/Addins/VC/AsmLoc.rc` and `SEAddin.cpp`.

`LiveTest.ps1` creates a disposable unsaved draft from the project reference template and restores the original active document afterwards. It passed 145 checks: 48 successive style changes across all four families, native selection/value/tolerance/text/placement preservation, precision/orientation checks, and a text-file edit applied on the next click. The external COM harness measured about 63 ms median per repeated change (first call about 257 ms); these are harness timings, not measurements of an installed ribbon button. The deployed handler runs inside SE and avoids cross-process startup. `../../Hammill Common/PackageCheck.ps1` verifies the combined add-in package, its starter style list, all four standalone macro installs and launcher discovery in isolated folders.

`Hammill Macro Catalog Check.exe` prints the folder, discovered macros and notices without starting Solid Edge or executing macros. It accepts `--settings PATH` and `--documents PATH` for isolated diagnostics. Without arguments it reads the user manager's actual settings.

`LiveToleranceTest.ps1` passed 233 native checks using a disposable project-local draft: all presets across inch, dual, vertical and inch-first dual styles; actual rendered upper/lower inch and metric numbers; exact precision changes; selection and model-value preservation; untouched shared styles; alternating the two cycle commands; and persistence/continued cycling after save/reopen. It reuses an existing Solid Edge session or creates a test session, restores the original active document, and closes only its own test document/session. These tests do not install or register the add-in; installed ribbon timing remains to be confirmed.

Compilation, regression checks, native dimension-style cycling, package checks and icon resource previews passed. **The v0.5 package has not been installed or registered by this test run.** The installed ribbon button's appearance and response still need confirmation after updating. Other live acceptance items remain Component Display before Update Views, clean shutdown, the standalone workflows/exports, and customized button/shortcut behavior after GUI changes.
