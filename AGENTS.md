# Project Guidelines

## Working Agreements
- Ask clarifying questions when the task scope is unclear.
- Keep pull requests small, focused, and reviewable.
- Do not modify files in the `vendor/` or `dist/` directories.

## Project Setup & Commands
- Use `pnpm` exclusively for package management (do not use `npm` or `yarn`).
- Run `pnpm lint` to check for style issues.
- Run `pnpm test` to execute the test suite before reporting completion.

## Code Style
- Use functional React components with hooks; avoid class components.
- Ensure all new features include appropriate unit or regression test coverage.
- When creating Solid Edge Macros refrence the examples found in the folder "SDK_2026_2510_English". Ensure you are using best practices for memory handling and overall macro stability and reliability
