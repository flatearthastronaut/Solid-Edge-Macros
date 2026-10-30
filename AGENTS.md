# Project Guidelines

## Working Agreements
- For future Solid Edge drafts, use the normal (new designs) draft template.
- Always use third-angle projection for drawings.
- Keep section views aligned with their parent/projected views.
- For any part first machined on a lathe, use the cap references as the layout standard: aligned end and longitudinal section views, grouped diameter banks, axial dimensions near their corresponding ends, and clear leaders; adapt the actual dimensions and notes to the part and operation.
- Match dimension presentation to `DraftingStandards/detailing example 1.dft`, using `DraftingStandards/ReferenceDimensionStyle.cs` and its audited property snapshot. Match Lines and Coordinate settings and the teal Driven dimension color (8421376); retain the job's required units, precision, and tolerances.
- Also match the reference's centerlines, center marks, cutting planes, text, leaders, callouts, surface-finish symbols, tolerance formatting, and title-block field styles. Use `DraftingStandards/ReferenceAnnotationStyle.cs` and `annotation-presentation-standard.xml`, choosing the appropriate annotation role and inch-only versus dual-unit Smart Depth format. Preserve actual limits, engineering content, feature links, section direction, and view alignment; enlarge/reposition text boxes when necessary instead of shrinking the reference text size.
- Add native Horizontal or Vertical constraints to all horizontal/vertical lines created in sketches, cutting-plane profiles, and other editable drafting geometry. Use each profile's local coordinates; do not substitute visually aligned endpoints for constraints.
- When a cutting plane passes through the model's Base coordinate-system origin, add a native point-on constraint between the model-linked projected Base origin and the cutting-plane segment. Use `DraftingStandards/BaseOriginConstraints.cs`; retain Horizontal/Vertical constraints and verify the Base connection after view movement and save/reopen. Matching coordinates alone is insufficient.
- Create chamfer callouts with native Chamfer Dimensions, not manually typed notes.
- Create hole callouts with the native Callout command, using model feature references and Smart Depth variables for hole/thread sizes and depths.
- Use two decimal places for counterbored-hole and bolt-circle dimensions, three for threads and hydraulic/air holes, and four for dowel-pin dimensions. Apply the precision to native dimensions and feature-linked callout formatting; preserve thread designations and explicit fit tolerances.
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
