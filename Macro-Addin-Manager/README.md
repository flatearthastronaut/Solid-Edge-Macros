# Solid Edge Macro & Add-in Manager v1.9

Run either executable on 64-bit Windows with .NET Framework 4.x. No Solid Edge interop DLLs are needed by the manager itself.

- **User:** `Compiled Executables/SE Macro Manager v1.9.exe` installs and updates programs from the shared folder. Categories are read-only; the category lock/unlock button has been removed.
- **Admin:** `Compiled Executables/SE Macro Manager Admin v1.9.exe` publishes local builds and assigns **Solid Edge Macros**, **Solid Edge Add-ins**, or **Windows Programs** in the table's Category dropdown. Changes are saved to the shared catalog immediately.

Both executables compile the same `ManagerForm` in `Program.cs`, artwork, category frames, grids, icon handling, path controls, and status footer. The `ADMIN` build flag selects publishing actions and different defaults; it does not fork the interface. Build both whenever changing the UI.

## Admin publishing

1. Open the Admin executable. **Local builds folder** defaults to the user's Documents folder plus `Codex\Solid-Edge-Macros` (on this workstation, `C:\Users\greg.lentner\Documents\Codex\Solid-Edge-Macros`). **Shared publish folder** defaults to `H:\Coop\internal\Macros and Add-ins`. Both can be browsed and are remembered separately from the user edition.
2. Click **Refresh**. The admin scans for `Compiled Executables` directories below the local folder. It skips `Exclude` case-insensitively, hidden dot directories, `tests`, and `SDK_*` trees before descending. If no output directories exist, the selected folder is treated as a distribution folder, using the existing EXE/ZIP/folder package rules.
3. Review **Local build**, **Published**, **Status**, and the selected row's source/destination details. The newest numeric release per family wins. A same-version ZIP is preferred over an EXE so its dependencies travel together. Matching ZIP families next to a project's `Build.cmd` are also considered. Identical ZIP copies within one project are deduplicated; conflicting sources from separate projects are blocked until resolved.
4. Set each program's **Category** in its dropdown. The row moves into the corresponding group. Shared-only programs can also be categorized without having their source build locally.
5. Select rows and click **Publish selected**, or use **Publish all new / changed**. Review the destination, versions, and categories in the confirmation dialog. Newly discovered programs are included in the latter action. Publication copies files; it does not run programs or registration scripts.

Existing ZIPs and loose EXEs keep their filenames and bytes. A package folder is published as a ZIP with the folder's filename and its internal relative layout; filesystem `Exclude` subdirectories are omitted. A root add-in registration suite in an output folder is packaged together using its parent project's release name. Existing ZIP payloads remain intact, including any internal Exclude entries, matching the user installer's existing behavior. **Loose EXEs publish alone: distribute a ZIP or package folder when a program requires supporting files.**

New and changed builds are staged on the share and verified before atomic replacement. Categories are saved before the build becomes visible. A locked destination or ordinary copy failure leaves the previous build intact and restores the previous categories. A shared publication lock serializes admin writes and category edits. Matching versions with another distribution format are blocked instead of creating ambiguous user catalogs, and newer shared versions cannot be downgraded. Older numbered releases and unrelated files on the share are retained; the user edition still removes older matching *local installed* versions after a successful update.

Interrupted publications preserve `.se-publisher/transaction-*` and block further admin writes. Review `recovery.xml`, `previous-build` when present, and `previous-categories.xml`. Restore the previous build to the journal's destination and the prior catalog to its category path if rolling back, or verify the completed publication before removing its transaction folder. An interruption during staging may leave only a payload with no journal; no live build has been changed at that stage. Recovery is manual, and user installation recovery under `.se-manager` remains separate.

## Shared categories

Assignments live in `.se-manager-categories.xml` at the shared folder root, keyed by program family so they follow filename version changes. Both editions reload the catalog on **Refresh**. Category edits reload and merge the latest file under the publication lock, preserving assignments made by other admins. User settings from v1.8 no longer override shared assignments. Programs without an admin assignment use the existing automatic classification until an admin assigns one.

The admin edition requires write permission to the shared folder. User installations require read access to the share and write access to their chosen local folder. Edition selection is a UI distinction, not an access-control system; restrict share write permissions to administrators using normal Windows/network permissions.

Admin paths and diagnostics use `%LOCALAPPDATA%\Coop\SEMacroManagerAdmin`; the user edition retains `%LOCALAPPDATA%\Coop\SEMacroManager`. Category assignments are not saved in either user's private settings file.

## Earlier versions

Version 1.8 skips filesystem directories named **Exclude**, case-insensitively, before traversing the available folder. This applies to nested source folders, package detection, icon lookup, classification, and files copied from source folder packages/shared suites. Existing local files are not deleted just because their directory is now excluded. ZIP package payloads remain intact; an Exclude directory on the source filesystem prevents discovering the ZIPs within it.

Version 1.7 matches Browse button height to the folder text fields and centers the four larger, bold action buttons. The Category button uses distinct bitmap padlock icons plus explicit Locked/Unlocked text, independent of emoji-font support.

Version 1.6 placed network/local folder controls side by side with Windows folder pictures, introduced three independent category frames and narrower columns, and replaced diagnostic controls with a short status footer. It used a category lock button and per-user choices; v1.9 replaces those with admin-assigned shared categories.

The ZIP icon decoder now reads PNG-compressed ICO frames directly instead of passing them through GDI Icon.ToBitmap, which was producing scrambled pixels. Icons render at 32 pixels with sufficient row height to avoid clipping. The actual BoltCircleHoles and SketchToModels icons were visually checked in the revised UI.

Version 1.5 introduced **Solid Edge Macros**, **Solid Edge Add-ins**, and **Windows Programs**. Shared add-ins and recognizable registration packages are categorized automatically. Other executable packages initially appear as macros because a Windows program and a macro can both be EXEs; in v1.9 the admin assigns desktop applications to Windows Programs. Category choices organize the list and do not override package registration requirements.

Windows EXEs, ZIPs, and portable application folders use the same version checks, icons, copy verification, update/repair actions, and removal of older matching versions. An EXE is copied rather than executed: MSI installation, setup-wizard automation, and Windows application registration are not performed. Put supporting files in the program's ZIP or package folder. Grouping does not move files into new source or destination directories. The three headings are always displayed and cannot be selected for installation; **Update all installed** applies across all groups.

Version 1.4 prioritizes ICO files inside the macro ZIP (including nested folders) over PNG previews and external sidecars. The exact selected ZIP entry is recorded in diagnostic logs. Icons are read directly from archive streams without extracting files.

Version 1.3 embeds the selected Macro Update icon (option A) in the executable and program window/taskbar. The ICO contains 16, 24, 32, 48, 64, 128, and 256-pixel images, generated from the retained artwork by `BuildIcon.ps1`. No separate icon file is required when distributing the executable.

Version 1.2 added package icons beside the package names. For ZIP packages, archive ICO files are preferred, then archive PNG files, then matching external artwork. Other packages use ICO/PNG artwork and then embedded executable icons. Local-only programs also display icons. Missing or unreadable icons use a standard application icon and diagnostic errors are logged. Shared suites use root SEAddIn artwork/resources instead of borrowing an unrelated component's icon. ZIP-contained executable icons are not extracted; include an ICO or PNG in the ZIP to display its custom icon. Icons load in the background during refresh, are cached for that catalog, and release file handles immediately so updates are not blocked.

Version 1.1 fixes a grid-selection crash that occurred while rows were being inserted or cleared, corrects the default source path, and added persistent diagnostics (the log controls were removed in v1.8).

## Operation

1. Select the **Available folder** (default `H:\Coop\internal\Macros and Add-ins`) and **Local folder** (default the current user's actual Documents folder plus `Add-ins`). Documents redirection is respected.
2. Click **Refresh**. The table lists available releases, installed versions, missing packages, updates, repairs, cleanup opportunities, and local-only programs. Select a row to see its location or shared add-in components.
3. Select packages and click **Install / update selected**. **Update all installed** updates, repairs, and cleans existing packages; it does not automatically install every missing program.
4. Close SE before an add-in installation. Approve Windows administrator prompts for the supplied registration scripts. The scripts run in their own directory; their windows remain visible so a script containing `pause` can finish.
5. After installing add-ins, start SE and check **Settings > Add-Ins** for availability and startup preferences.

Both paths persist per user in `%LOCALAPPDATA%\Coop\SEMacroManager\settings.xml`. Selecting a folder or refreshing saves them, even if the network source is temporarily offline. Editing a path clears the old results until refreshed.

Saved settings still using the old shipped source `H:\solid_edge\Coop\internal\Macros and Add-ins` automatically migrate to the corrected default. Other saved source paths are preserved.

## Diagnostic logs

Logs remain automatic and local at `%LOCALAPPDATA%/Coop/SEMacroManager/Logs` for troubleshooting. The manager has no Open logs or Export logs controls and does not display its diagnostic stream. The footer shows concise operation status and errors. Logs are not transmitted automatically.

Logs record versions, timestamps, paths, scan results, the precise icon file/ZIP entry selected, verified copies, registration exit codes, transaction recovery, and full exceptions. Elevated registration-script console output is not captured. Logs rotate at approximately 5 MB, retaining up to 30 parts. A logging failure does not interrupt installation or rollback.

## Supported layout

- A loose EXE is a macro. Its version comes from its filename, for example `Dual Dimension Toggle v1.9.exe` or `BoltCircleHoles-v0.20.exe`. For macros with dependencies, distribute a ZIP or folder instead of a loose EXE.
- ZIPs are extracted into a matching local package directory, retaining their internal layout and supporting files. The ZIP itself is not retained in the destination. The executable may be inside another directory if the archive contains one.
- A directory containing program files is a package; its directory name supplies the release version.
- A root `Register.cmd` or `SEAddIn.dll` identifies a **shared SE add-in suite**. Its non-hidden add-in subfolders and root supporting files are copied together. Root EXEs, ZIPs, and folders containing EXEs without a `Register.cmd` are treated as independent macro packages. This also keeps installed ZIP macros visible alongside a shared suite. If an add-in folder contains helper EXEs but no registration script of its own, the package layout needs review before deployment. The suite appears as one selectable row; its component directory names and matching installed directory names appear below the table. Individual components of a shared suite cannot be selectively installed without first establishing their registration/dependency boundaries.
- Independently packaged add-ins can be selected independently. They require an unambiguous `Register.cmd` and matching `UNRegister.cmd` in the same directory. A root registration script takes precedence over subordinate scripts in a shared suite.
- Same-name/same-version source alternatives, such as both a ZIP and an EXE, are marked ambiguous. Keep one distribution format for each release in the selected source folder. Numeric version segments are compared as integers, so 1.10 is newer than 1.9 and 1.0 equals 1.0.0. Versions containing prerelease suffixes are not interpreted as numbered releases.
- Unversioned add-ins use file content comparison; no version number is invented.

## Registration contract and current validation limit

The supplied `Addin Help.docx` describes running `UNRegister.cmd` before changing add-ins and `Register.cmd` afterward, as administrator, with SE closed. The manager follows that workflow. It deliberately does not guess whether a DLL needs RegAsm, regsvr32, a particular .NET runtime, or a CLSID.

**The actual H: packages and command files were unavailable during development. Live SE registration has not been tested.** Supply those command files for review before production rollout. Scripts must target the local package, use their own directory or relative paths, and return a nonzero exit code on registration failure. A script which masks a failed command with a successful final command cannot be verified from its exit code. The manager reports script completion, not independent confirmation that SE loaded the add-in. Hard-coded paths in a vendor script must be corrected before using selectable install paths.

The SDK examples reviewed were `SDK_2026_2510_English/SDK/Advanced/samples/Addins/VC/regSeAddin.cpp` and the VB.NET `SEAddIn/SEAddIn/Addin.vb` sample. They demonstrate that SE registration includes component categories and class/environment metadata, not simply copying DLLs. This manager does not attach to SE or retain COM objects; file, ZIP, hash, process, and installation-lock handles are disposed deterministically.

## Copy, cleanup, and recovery

New files are staged under the local `.se-manager` directory and verified with SHA-256 before registration. An exclusive installation lock prevents simultaneous manager instances from writing the same destination. Existing files and clearly older matching versions are moved to transaction backups, then new files are installed and registered. Successful transactions delete those backups, removing the older versions.

Only matching version families and previously tracked files are candidates for cleanup. Local-only packages are retained. A newer local numbered version is never downgraded. Unversioned aliases are retained when their age cannot be established. Unknown supporting files are retained. Old versioned package directories are removed as whole packages, including their contents; do not store personal files inside obsolete version directories. Shared suite components still present in the source are retained.

Managed file manifests allow content-change detection and repair. Removed supporting files are deleted only if they are unchanged from the previous managed copy; modified removed files are preserved. Folder links/junctions, overlapping source/destination folders, ZIP traversal, duplicate ZIP paths, and archives expanding beyond 4 GB are rejected.

On an ordinary copy or registration failure, the manager attempts to unregister the new add-in, restore the old files, and re-register the old add-in. A failed recovery or interrupted process preserves `.se-manager/transaction-*`, including `backup` and `recovery.xml`, and blocks further installation. Do not delete this folder to dismiss the error: with SE closed, an administrator should inspect the journal, restore backed-up paths to their original local locations, remove incomplete new files, and run the restored registration script. Only after recovery is verified should the transaction folder be removed. Unexpected process termination does not trigger automatic recovery.

## Build and regression checks

Run `Build.cmd` to compile **both** executables into `Compiled Executables`; run `Test.cmd` for the isolated regression harness. Tests use fake registration and files under `tests/work`, never the H: share or the user's installed add-ins. Tests cover numeric comparison, installation status, old-version cleanup, shared suites, ZIP dependencies and traversal, missing scripts, SE-running checks, ambiguous source versions, overlapping paths, interrupted recovery, downgrade prevention, obsolete supporting files, locked files, and rollback/re-registration.

Publishing tests additionally cover recursive build discovery, ZIP preference, duplicate builds, retained older shared releases, dependency packaging, Exclude pruning, content comparison, replacement failure, category rollback, concurrent publisher locks, shared-format conflicts, malformed catalogs, shared-only categorization, category changes across versions, stale private settings, and both editions of the real shared form. Tests display invisible forms only and never run supplied registration scripts. The v1.9 suite passed outside the sandbox after Windows file replacement was denied by the restricted test environment; no live network publication or Solid Edge registration was performed.
