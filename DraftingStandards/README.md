# Solid Edge drafting standard

The user-provided `detailing example 1.dft` is the appearance reference. It is a copy; the original is unchanged. Forty-six dimensions were inspected on 2026-10-28.

Use `ReferenceDimensionStyle.Apply(dimension.Style)` for new dimensions. The 142 presentation properties in `dimension-presentation-standard.xml` reproduce the reference, including all readable Lines and Coordinate properties, Arial 0.125-inch text, native terminator size 1, line width 0.3 mm, extension/offset gap factors 0.5, and driven color 8421376 (RGB 0,128,128). These native gap values are ratios, not lengths to convert to inches.

The reference uses metric primary/inch secondary dimensions and two precision styles. Preserve each job's required units, rounding, dual-unit display, dimensional scale, and numeric tolerance values. The fitting remains an inch drawing. Where reference dimensions have individual presentation overrides, use the most common value: leader length 1.5 and break-line gap disabled. Do not change dimension geometry, text overrides, prefixes, suffixes, associations, or native dimension types.

`AxisConstraints.Ensure` attaches a native Horizontal or Vertical relation to axis-aligned editable Line2d geometry, using its profile's local coordinates. It checks for existing relations and verifies that adding one does not move the endpoints. Invoke it for sketch lines, construction axes, cutting-plane profile lines and other new editable drafting lines. Sloped lines and zero-length geometry are excluded; projected model geometry is not a sketch to constrain.

Both tools use the SDK STA/message-filter pattern. Build with `Build.cmd`; executables are in `Compiled Executables`. The fitting's build includes the same shared standards. `--test` checks axis classification. `--apply` updates existing fitting dimension presentation in place and checks all properties after reopening. `--constraints` checks/adds cutting-plane relations in both existing fitting drafts and verifies saved relations. Running without arguments performs the reference/fitting audit only. The reference is never saved.

Verified results: seven fitting dimensions, including two native chamfers, match all 142 presentation properties after save/reopen. Units, precision, numeric tolerances and measured values are retained. Three cutting-plane lines across the two fitting drafts have persistent native axis constraints. Existing model sketches were not retroactively changed; future sketch creation uses the shared constraint helper.
