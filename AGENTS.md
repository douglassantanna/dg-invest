# CRITICAL RULES - MUST FOLLOW

## RESPONSES

- Keep responses concise and to the point - unless the user asks otherwise
- Read `prompt.md` for product context, target architecture, and delivery roadmap.

## PLANNING MODE

- Ask clarifying questions only when requirements, design, or scope cannot be safely inferred from the codebase and request.
- Inspect the codebase before proposing a plan; do not assume the stack, architecture, or existing behavior.
- Use focused sub-agents for independent or broad research when they improve confidence or speed.
- For complex plans, independently review key risks before presenting them.

## CHANGE / EDIT MODE

- Delegate independent work that can safely run in parallel; otherwise implement the smallest correct change directly.
- When using sub-agents, coordinate their work and verify the combined result.
- Use the model and tools appropriate to the task complexity.
- After a change, run the relevant available validation: `dotnet test` or `dotnet build` for backend changes, and `npm run test` or `npm run build` for Angular changes.

## DATABASE SCHEMA CHANGES

- This project uses Entity Framework Core, not Drizzle.
- For schema changes, generate and inspect an EF Core migration. Apply database migrations only when the user requests it or the test workflow requires it.

## TESTING

- Use test-driven development for behavior changes: write a failing test first, implement the minimum change to make it pass, then refactor.
- Use any testing tools, libraries available to the project for testing your changes
- Never assume your changes simply work, always test!
- If the project does not have any testing tools, scripts, MCP tools, skills, etc. available for testing, ask the user whether testing should be skipped.

## UI DESIGN

- Preserve the existing application's visual patterns and follow any design requirements provided by the user.
- For new or substantially redesigned UI, use the `frontend-design` skill and verify responsive behavior, keyboard focus, and reduced-motion support.
