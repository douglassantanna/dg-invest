# Coding standards

- This application serves approximately 20 users; prefer the simplest maintainable implementation and clarify only when the request and code leave no safe inference.
- For schema changes, generate and inspect an EF Core migration; apply it only when requested or required by the test workflow.
- Run relevant checks when requested. Otherwise, do not run tests or builds. For behavior changes, use TDD: failing test, minimal fix, refactor.
- Preserve existing UI patterns and requested design. For new or substantially redesigned UI, use `frontend-design` and verify responsive behavior, keyboard focus, and reduced motion.
