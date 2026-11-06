# Hammill O-Ring Finder v1.0

Standalone x64 Windows macro extracted from the behavior of the July 2019 `SEAddIn.dll`. It connects to a running Solid Edge session; no add-in registration or legacy DLL is required. Requires .NET Framework 4.x. Target host: Solid Edge 2026.

## Install and use

Extract **O-Ring Finder v1.0.zip** into a local folder, keeping all five files together. Open an assembly in Solid Edge and run **O-Ring Finder v1.0.exe**. Select an O-ring part, review the size and pressure option, and click **Apply & save part**. Close or select another part without applying to leave it unchanged.

The ZIP can also be published and installed as **Solid Edge Macros** through Macro Manager v1.14. The manager creates the stable **O-Ring Finder/O-Ring Finder.exe** and copies the table beside it. Restart Solid Edge after installation for Hammill Macro Launcher discovery. No Register.cmd or administrator registration is needed.

## Editable table

The macro loads **Oring_table.txt beside the EXE actually being run**, independent of the current working folder. For a Macro Manager installation, edit the table in the stable, unversioned folder, not the numbered copy. The window shows the complete table path.

Each nonblank row has three comma-separated fields, using inches and decimal points:

```text
2-001,1/32,.029
2-002,3/64,.042
2-003,1/16,.056
```

The fields are part number, nominal cross-section width, and inside diameter. Width accepts a fraction or decimal; part numbers retain the `2-001` format used by the existing BOM properties. Blank lines and lines beginning with `#` are allowed. Rows may be reordered. Duplicate part numbers, duplicate width/ID combinations, missing columns and invalid/nonpositive numbers stop the scan with a line number. There is no embedded table or silent fallback.

Save your changes and click **Reload table / assembly**, or reopen the macro. No recompilation is needed. The file is also reloaded before applying a choice; if its result has changed, the macro displays the new result for review before permitting that write.

**Choose table…** selects a different external TXT for the current session. To consistently use a separately maintained/shared table, start the macro with `--table "full path to Oring_table.txt"`. The macro only reads the table and never edits it. Back up a customized table before updating or repairing its package in Macro Manager: the manager replaces packaged files. Keeping the authoritative table outside that package avoids replacement during updates. Building this project preserves an existing table in Compiled Executables.

## Preserved workflow

- Inspect top-level assembly occurrences whose names begin with `O_` or `O-`, case-insensitively. Process each referenced part file once. Subassembly recursion is not enabled, matching the original tool.
- Read profile dimensions named `ID` and `WD`. Convert Solid Edge metres to inches using the exact 0.0254 factor. ID is rounded to three places for matching, as in the original.
- Infer nominal width from the existing WD rules: .090/.098/.0955 → 1/16; .120/.145/.1425 → 3/32; .160/.192/.1895 → 1/8; .240/.286/.2835 → 3/16; .320/.380/.3775 → 1/4. Original three/four-place comparisons are retained. Editing catalog sizes does not change these groove-width rules.
- Offer the largest table ID no greater than the part ID, plus the next smaller size **within the same nominal width**. The original streaming search could cross a width-group boundary or carry stale state; this version filters and sorts the group first. Below-minimum IDs stop matching; above-maximum IDs offer the largest listed sizes. An exact match is allowed.
- Preserve the original percentage calculation, displayed as **Difference**: `(1 − ring ID / part ID) × 100`, rounded to a whole percent. It is not the alternative stretch calculation with ring ID as the denominator. This is a catalog lookup using existing shop rules, not a new seal-design qualification.
- Preserve the pressure choices and property text: `drawing number = PARKER# 2-xxx`; `dave_b = TORINGxxx`. The > 1,200 psi option appends ` P4300` and `-P4300`, respectively.

Both custom properties must already exist. Nothing is written until **Apply & save part** is clicked. That action saves the part, including other unsaved changes in it; it does not edit model geometry or save the assembly. The document reference and dimensions are rechecked before applying. A failure attempts to restore both old property values in memory and reports that the part needs checking; a failed CAD save cannot be treated as a disk transaction.

## Build and verification

Run `Build.cmd`, then `Test.cmd`. Output EXE/ZIP and runtime table are in **Compiled Executables**. Run `tests/PackageCheck.ps1` for an isolated installation/discovery check against the repository's actual Macro Manager v1.14 and Hammill Macro Launcher. No installed Solid Edge interop assemblies are needed for building: late-bound automation follows the supplied SDK `SolidEdge.Variables` and `SolidEdge.PropertySets` examples. All automation runs in an STA with bounded OLE retries. Collection access is indexed and each acquired COM reference is released once; the macro never closes Solid Edge or its documents.

All 45 regression checks passed, covering the actual external table, live table edits/reloads, group boundaries, units, duplicate instances, dimension errors, property text and rollback, and form behavior using fake CAD objects. The real-manager package installation and launcher discovery check also passed, and the form was rendered and visually checked. Test work remains under `tests/.work`. Live acceptance in Solid Edge is still needed for real assembly scanning and property/save behavior. The original BWH DLL has not been modified or unregistered; O-Ring Finder is now scoped as a standalone macro in the Hammill rebuild plan.
