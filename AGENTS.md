# Project Guidelines

## Working Agreements
- For future Solid Edge drafts, use the normal (new designs) draft template.
- Start drawings at 1:1 scale on a B-size sheet. Enlarge the view scale if needed for uncrowded dimensions. C, D, E, and EE sheets may be used when necessary, but ask the user before changing sheet size.
- Always use third-angle projection for drawings.
- Keep section views aligned with their parent/projected views.
- Isometric drawing views should be shaded. Add centerlines/center marks to the orthographic and section views, not to the isometric view.
- For any part first machined on a lathe, use the cap references as the layout standard: aligned end and longitudinal section views, grouped diameter banks, axial dimensions near their corresponding ends, and clear leaders; adapt the actual dimensions and notes to the part and operation.
- Start with linear dimensions of outside features above the section and linear dimensions of internal features below it. Place outside-feature lengths below only when the upper dimensions become crowded. Select extension origins on the corresponding side of the view.
- Match dimension presentation to `DraftingStandards/detailing example 1.dft`, using `DraftingStandards/ReferenceDimensionStyle.cs` and its audited property snapshot. Match Lines and Coordinate settings and the teal Driven dimension color (8421376); retain the job's required units, precision, and tolerances.
- Attach manufacturing dimensions to physical model geometry, excluding coincident hidden cutting-plane/Base construction graphics. Verify the parent edges and extension origins after reopening; a correct measured value alone does not prove a correct attachment.
- Also match the reference's centerlines, center marks, cutting planes, text, leaders, callouts, surface-finish symbols, tolerance formatting, and title-block field styles. Use `DraftingStandards/ReferenceAnnotationStyle.cs` and `annotation-presentation-standard.xml`, choosing the appropriate annotation role and inch-only versus dual-unit Smart Depth format. Preserve actual limits, engineering content, feature links, section direction, and view alignment; enlarge/reposition text boxes when necessary instead of shrinking the reference text size.
- Add native Horizontal or Vertical constraints to all horizontal/vertical lines created in sketches, cutting-plane profiles, and other editable drafting geometry. Use each profile's local coordinates; do not substitute visually aligned endpoints for constraints.
- When a cutting plane passes through the model's Base coordinate-system origin, add a native point-on constraint between the model-linked projected Base origin and the cutting-plane segment. Use `DraftingStandards/BaseOriginConstraints.cs`; retain Horizontal/Vertical constraints and verify the Base connection after view movement and save/reopen. Matching coordinates alone is insufficient.
- Create chamfer callouts with native Chamfer Dimensions, not manually typed notes.
- For repeated equal-size chamfers or radii/fillets, create only one native dimension per size and set its Dimension Prefix Subfix to `TYP(x)`, where x is the total number of physical occurrences. Count continuous revolved features once, not twice because they appear on both sides of a section.
- Create hole callouts with the native Callout command, using model feature references and Smart Depth variables for hole/thread sizes and depths.
- Include the thread machining diameters (external major and internal minor diameters as applicable). External thread diameters are modeled nominally; set native upper tolerance to -.002 inch and lower tolerance to -.006 inch, both negative. Do not change the nominal model diameter to represent the manufacturing limits.
- When the user specifies a dimension as two-place, three-place, etc., this always means the number of digits after the decimal point in the inch value. Do not change metric precision to match that instruction.
- Use two inch decimal places for counterbored-hole and bolt-circle dimensions, three for threads and hydraulic/air holes, and four for dowel-pin dimensions. Apply the precision to native dimensions and feature-linked callout formatting; preserve thread designations and explicit fit tolerances.
- Chamfer and radius/fillet inch values use two decimal places, except values of .015 inch or less use three. Preserve metric precision independently.
- Ask clarifying questions when the task scope is unclear.
- Keep pull requests small, focused, and reviewable.
- Do not look into files outside of the project directory and its sub-directories
- When making changes to macro programs compile them after making modifications and drop the executable file in a sub-directory called "Compiled Executables"
- After compiling a new version of the macro add all files you've changed to a git commit using the command "git add .". Then execute "git commit -m "XXXXXXX" where XXXXXXX is a short description of changes made. Then push the commit using "git push origin main".

## Code Style
- Ensure all new features include appropriate unit or regression test coverage.
- When creating Solid Edge Macros refrence the examples found in the folder "SDK_2026_2510_English". Ensure you are using best practices for memory handling and overall macro stability and reliability
- Ensure that you code is efficent and contains detailed comments explaining how the code works and why you made certain decisions.
- Whenever possible make sure your code is using compute power efficently.
