# Update Dimension Styles v1.0

Open an existing draft, run this macro, review the missing styles, then click **Add missing styles**. Save the drawing when satisfied. The macro does not save the target automatically, overwrite existing styles, or reassign existing dimensions. Check is read-only; if all required names already exist, no master document is opened and nothing is imported.

The required names come from the same **Hammill.DimensionStyles.txt** in Macro Manager's shared Available/source folder that Cycle Places uses. The list is re-read before checking or applying. No independent copy of the cycle list is packaged. Style-name matching follows the cycle command's case-insensitive, trimmed-name rules. Invalid or ambiguous names block repair.

The default master is **normal (new design).dft** beside the macro executable. This package includes Greg's supplied standard. Browse can select a different master for the current session. All missing required names must exist in the master before any import is attempted. Native ImportStyles2 imports dimension styles with replacement disabled; unused newly imported styles outside the cycle list are removed. Any additional imported styles Solid Edge refuses to remove are reported and retained. Pre-existing styles, including unrelated styles, remain untouched. The active style is restored. The macro closes only a master it opened itself and restores the target document.

This checks presence, not whether existing styles have the latest formatting. Existing dimensions and local style customizations are deliberately preserved. A failed native import can leave partial additions; the report tells the user to review before saving. Do not use the master itself as the target drawing.

## Master cleanup

On November 17, 2026, the supplied project master was backed up and these highlighted styles were removed using Solid Edge's native collection method: **ANSImm, BSI, DIN, ISO, JIS**. Save/reopen confirmed the removals. Solid Edge initially returned E_FAIL for **ANSI, GRINDSTOCK, Z1**. Subsequently, eight grouped surface-finish symbols on **Notes To Copy** were reassigned from Z1 to ANSI at Greg's request, and **Z1 was removed**. ANSI and GRINDSTOCK remain. GRINDSTOCK still resisted removal in disposable copies after clearing drawing objects, blocks, most sheets and tables, then saving/reopening; its exact dependency remains unresolved. **Fraction** was not highlighted and was retained. The active style and the 12 configured cycle styles were preserved. The correct vertical two-place name in this master is **3 2 place (vert)**.

`Backups/normal (new design) before cleanup.dft` preserves the original supplied master. The cleaned copy at the repository root and the bundled copy contain the same styles. This does not replace the company's production template on its network share.

## Distribution and verification

Build with `Build.cmd`; install **Compiled Executables/Update Dimension Styles v1.0.zip** through Macro Manager. Keep the master draft beside the EXE. Hammill's launcher discovers the macro after restarting Solid Edge. The build also supports the normal Tooltips folder and automatic WMV conversion.

`Test.ps1` runs isolated regression tests. `Test.ps1 -Live` repairs a disposable project-local copy of the master, verifies preserved existing formatting, native vertical style precision, save/reopen, and a no-op rerun. It never edits or saves the user's open drawing. `tests/AuditMaster.ps1` reports styles; its explicit `-Clean` option performs the requested eight-name cleanup only on the supplied project master, backing it up first.

API basis: Siemens DraftDocument.ImportStyles2 with igDimensionStyle=0 and bReplace=false, DimensionStyles.Remove, and the bundled SDK COM lifetime/message-filter pattern. The master supplies complete native style formatting; the macro does not infer it from a name.

