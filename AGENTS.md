# Project Guidelines

## Working Agreements
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
