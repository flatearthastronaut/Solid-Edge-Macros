# Solid Edge drafting standard

## Dimension placement and visual review

Use the user-supplied CAP_19679-actual.dft as the cap layout reference. A valid
model link does not establish a readable drawing. Check the rendered sheet after
saving and reopening, not only the dimension counts and numeric values.

Apply ASME Y14.5-2018 section 4.4: group and align related dimensions; place
shorter dimensions nearer the outline and larger dimensions farther out; minimize
dimension/extension-line crossings; keep text clear of lines and arrowheads.
Use 10 mm to the first dimension and 6 mm between parallel dimensions as spacing
guides, allowing more room for stacked dual-unit limits. These are readability
guides, not an automatic rejection test. Use short, direct leaders; neighboring
leaders should run parallel where practical. Prefer dimensions outside the view,
with exceptions where placing them inside communicates more clearly.

For caps, organize diameter dimensions into nested banks beside the section,
put axial dimensions on the nearest appropriate end of the section, stagger
stacked limit text, and keep chamfer/radius leaders close to their features.
Do not blindly move a dimension to the other end while leaving its extension
origins attached across the full view. Preserve engineering values and ask before
adopting different manufacturing instructions from a layout reference.

Sources: [ASME Y14.5-2018, sections 4.4.1, 4.4.2.1, 4.4.4 and 4.4.8](https://calprodglobal.com/wp-content/uploads/2023/04/ASME-Y14.5-2018-Dimensioning-and-Tolerancing.pdf);
[ASME Y14.2, Line Conventions and Lettering](https://www.asme.org/codes-standards/find-codes-standards/line-conventions-and-lettering);
[ASME Y14.100, Engineering Drawing Practices](https://www.asme.org/codes-standards/find-codes-standards/engineering-drawing-practices).

The user-provided `detailing example 1.dft` is the appearance reference. It is a copy; the original is unchanged. Forty-six dimensions were inspected on 2026-10-28.

Use `ReferenceDimensionStyle.Apply(dimension.Style)` for new dimensions. The 142 presentation properties in `dimension-presentation-standard.xml` reproduce the reference, including all readable Lines and Coordinate properties, Arial 0.125-inch text, native terminator size 1, line width 0.3 mm, extension/offset gap factors 0.5, and driven color 8421376 (RGB 0,128,128). These native gap values are ratios, not lengths to convert to inches.

The reference uses metric primary/inch secondary dimensions and two precision styles. Preserve each job's required units, rounding, dual-unit display, dimensional scale, and numeric tolerance values. The fitting remains an inch drawing. Where reference dimensions have individual presentation overrides, use the most common value: leader length 1.5 and break-line gap disabled. Do not change dimension geometry, text overrides, prefixes, suffixes, associations, or native dimension types.

`AxisConstraints.Ensure` attaches a native Horizontal or Vertical relation to axis-aligned editable Line2d geometry, using its profile's local coordinates. It checks for existing relations and verifies that adding one does not move the endpoints. Invoke it for sketch lines, construction axes, cutting-plane profile lines and other new editable drafting lines. Sloped lines and zero-length geometry are excluded; projected model geometry is not a sketch to constrain.

Both tools use the SDK STA/message-filter pattern. Build with `Build.cmd`; executables are in `Compiled Executables`. The fitting's build includes the same shared standards. `--test` checks axis classification. `--apply` updates existing fitting dimension presentation in place and checks all properties after reopening. `--constraints` checks/adds cutting-plane relations in both existing fitting drafts and verifies saved relations. Running without arguments performs the reference/fitting audit only. The reference is never saved.

Verified results: seven fitting dimensions, including two native chamfers, match all 142 presentation properties after save/reopen. Units, precision, numeric tolerances and measured values are retained. Three cutting-plane lines across the two fitting drafts have persistent native axis constraints. Existing model sketches were not retroactively changed; future sketch creation uses the shared constraint helper.

## Other annotations

`annotation-presentation-standard.xml` stores the reference's role-specific styles. `ReferenceAnnotationStyle` applies centerline/center-mark styles, cutting-plane graphics, leader/callout properties, note text, title fields, surface-finish and tolerance-frame appearance. Dimensional tolerance appearance remains covered by `ReferenceDimensionStyle`; numerical tolerance limits are never copied from the example.

The working reference provides 12 centerlines, 56 center marks, 13 callouts, one cutting plane, eight surface-finish symbols, and seven frames. Its `Notes To Copy` sheet supplies ordinary bold Arial notes; its `B-Size` background supplies Arial Narrow title fields and the distinct black static labels. Standalone leader examples come from `EXPANSION FORM`. The six red +/- frames are treated as tolerance notes, not the default for every geometric tolerance frame. Cutting-plane captions are 0.25-inch Arial, teal, with terminator ratio 2 and 0.6-mm chain lines. Other ordinary annotations use 0.125-inch text and 0.3-mm line width.

The fitting uses the reference's inch-only Smart Depth recipe (`%TD DP.`), avoiding duplicate depths from forcing the reference's dual-unit recipe onto an inch-only drawing. Model-linked feature variables and text content remain intact. Text boxes are widened or positioned to fit the matched text sizes; the title-block fields use the reference's Arial Narrow font. The shared document method contains fitting-specific text-layout and B-size field mapping; adapt those mappings when applying it to a different sheet layout.

`--annotations` audits annotations without saving documents. `extract-annotation-standard.py` reproducibly extracts the property standards. `--apply-annotations` updates both existing fitting drafts, verifies all selected properties after reopening, checks unchanged contents, numerical tolerances, model attachment states, section direction, view placement and constrained cutting-plane geometry, and refreshes preview images. Latest results are in `annotation-verification.txt`. No reference or company template is modified.

## Cutting planes through Base CSYS

Use `BaseOriginConstraints.Ensure(cuttingPlane)` after ending the new cutting-plane profile edit. It checks whether a profile segment passes through the projected Base origin, exposes the Base model member when necessary, and adds `Relations2d.AddPointOn(originPoint.Reference, 0, line)`. The origin is a model-linked `DVPoint2d` belonging to the `Base` coordinate-system model member; the `CoordinateSystem2d` coordinate-query wrapper itself is not a constrainable graphic entity. No fixed sheet point is substituted. Native Horizontal/Vertical relations remain in place. Offset segments are left unchanged, and existing Base relations are detected to avoid duplicates.

`--connect-origins` updates both fitting drafts in place. All three cutting planes have verified native point-on relations to Base. The validation moves each source view, checks the live relation, restores the original placement, and verifies relationship targets after save/reopen. Dimensions, view alignment, constrained cutting-line geometry and annotation presentation are checked for preservation. Results are in `base-origin-constraint-verification.txt`. The Base origin graphics remain available to the relationship solver.
