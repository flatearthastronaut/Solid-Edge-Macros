# Sketch to Models v2.31 — SDK-reviewed rebuild

Built from the working v2.30 source, preserving its two modeling workflows and latest color rules. Live end-to-end Solid Edge validation is still required.

## Run

Close older macro instances and run Compiled Executables/SketchToModels-v2.31.exe with a saved assembly active. If your ribbon references an older versioned EXE, update the button to this EXE. The matching PNG is provided for its ribbon icon.

Assembly filename: exactly one isolated five-digit drawing number. Output: "<sketch name> <number>.par" beside the assembly, using the chosen normal.par template. Existing files are never overwritten.

Standard workflow: default placement, linked sketch, Right (yz) solid revolve, automatic part-Z axis, Project to Sketch and Wireframe Chain.
Bushing Style: graphic coordinate-system placement, linked sketch, Feature's Plane revolved surface with automatic local-Z centerline, then Right (yz) solid revolve with user-chosen axis.
After Finish, Continue validates and advances; finished parts and the assembly are saved. Failed or canceled work is preserved.

## Existing color styles only

Collet / diaphragm: Yellow. Part stop / partstop: Green.
Adapter: Khaki. Puller: Orange. Body: Bronze. Insert / inserts: Blue.
Case-insensitive contained-name matching ignores spaces, hyphens and underscores. Missing styles are skipped. No styles are created or modified.

## SDK-based changes

- Added ComSupport.cs: require STA when connecting, translate the SDK's MK_E_UNAVAILABLE case into an actionable message, preserve other COM failures.
- Register the OLE message filter with HRESULT checking; retain it during the UI loop and restore the previous filter with a disposable scope on the same thread.
- Dispose the main form explicitly. Clear completed grid sketch references, and release the form's retained COM references once when it is disposed. Cleanup does not close documents or quit Solid Edge.
- Follow the SDK warning about shared wrappers: no FinalReleaseComObject, no release-to-zero loops, and no forced collection while modeling. This is targeted lifetime improvement, not a claim that every temporary dynamic COM reference is deterministically released.
- Use 64-bit-safe conversion for application HWND values.
- Use the bounded native Wireframe Chain selector for both solid and surface stages, avoiding the solid stage's potentially blocked UI Automation traversal. This is a project-proven reliability change, not a technique prescribed by the SDK.
- Report an initial SaveAs failure as a save-stage failure.

## SDK evidence

All paths below are relative to the parent Solid-Edge-Macros repository:
- SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Automation/SolidEdge.Automation/Program.cs — STA startup, running-instance connection, specific unavailable-server error handling.
- SDK_2026_2510_English/SDK/API_Samples/Samples/cs/SolidEdge.Automation/SolidEdge.Automation/OleMessageFilter.cs — apartment requirement, message filter registration/revocation, busy-call handling.
- SDK_2026_2510_English/SDK/Advanced/samples/toolbars/vb.net/Project1.NET/Toolbars.vb — release retained COM references and clear fields.
- SDK_2026_2510_English/SDK/Advanced/samples/Addins/VB .NET/SEAddIn/SEAddIn/Addin.vb — warning that FinalReleaseComObject can affect other consumers of a shared wrapper.

The existing bounded 15-second busy retry is retained rather than copying the sample's unbounded retry behavior. Typed revolve API signatures, checkpoint saves and user-tested command ordering are retained.

## Build and tests

Run Build.cmd on Windows with Solid Edge 2026 installed. Executables, template settings, and ribbon images are placed in Compiled Executables, per the repository AGENTS.md. It compiles SketchToModels.cs and ComSupport.cs using the 64-bit .NET Framework compiler and the installed Siemens interop library.
SketchToModels-Commented.cs mirrors the main source for reading. Do not compile both copies together.
Run Test.ps1 for ten regression programs. All passed in this rebuild: COM lifetime/thread guards, naming/color rules, Continue with blocked worker, incomplete-feature guards, Z-axis geometry, surface projection, placement lookup, manual-axis behavior, and separate-process native dropdown selection.

Tests use mock CAD objects and real Windows dropdown controls. They do not verify live model geometry, saving, or visual appearance in Solid Edge. Retest one standard and one Bushing Style part before adopting this version for a batch.

## Known limitations

Projection relationship recognition is incomplete. Bushing Style allows an unrecognized relation when the part still has an interpart link and logs a warning; this does not certify full profile associativity.
The native selector verifies a dropdown value, not the feature result. The feature checks remain separate.
A previously loaded part with the same name can block SaveAs even when the file no longer exists; preserve unsaved work before clearing that session.
Computer Use screen capture previously failed on this machine; the screenshotter remains useful for reviewing manual tests.

## Repository workflow
The current AGENTS.md requires building after modifications, storing runnable files under Compiled Executables, then git add ., a descriptive commit, and git push origin main. Test-generated binaries and logs are ignored; release executables are included.

