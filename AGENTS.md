# AGENTS.md

## Project

FrameFileTool is a Windows WPF desktop app for image sequence file operations.

The app should remain a maintainable tool platform, not a one-off script. New features must keep UI, application state, planning logic, and file execution logic separated.

## Development Rules

Use these rules for every change in this repository:

- Use TDD for core behavior.
- Follow SOLID principles.
- Prefer functional programming style for planning and transformation logic.
- Keep WPF UI code thin.
- Keep file-system side effects isolated.
- Do not put business logic in event handlers or code-behind.
- Do not add broad abstractions until there are at least two real use cases.

## Architecture

The intended architecture is:

```text
Views
  WPF XAML and minimal code-behind only.

ViewModels
  UI state, commands, validation, and calls into services.

Services
  File scanning, operation planning, and execution.

Models
  Immutable or simple data objects used across services and view models.
```

Code-behind may initialize dependencies and set `DataContext`. It should not contain file operation rules, rename rules, or frame deletion rules.

## TDD Requirements

Core behavior should be test-first whenever practical.

Write tests for:

- Natural file sorting.
- File filtering by extension.
- Frame delete planning.
- Rename planning.
- Conflict detection.
- Per-folder behavior when subfolders are included.
- Edge cases such as empty folders, invalid intervals, duplicate target names, and existing target files.

Preferred workflow:

```text
1. Write or update a failing unit test.
2. Implement the smallest production change.
3. Run tests.
4. Refactor while keeping tests green.
```

Do not rely on manual GUI testing for business rules. GUI testing can supplement unit tests, but it must not be the only verification for file operation logic.

## SOLID Guidelines

### Single Responsibility

Each class should have one clear reason to change.

Examples:

- `FileScanner` scans and sorts files.
- `FrameDeletePlanner` decides which files should be deleted.
- `RenamePlanner` creates rename plans and detects rename conflicts.
- `FileOperationExecutor` performs file-system side effects.

Do not mix planning and execution in the same method.

### Open/Closed

Add new tools by introducing new planners, view models, or services rather than modifying unrelated logic.

For example, a future image format conversion feature should not change `FrameDeletePlanner`.

### Liskov Substitution

If interfaces are introduced later, implementations must preserve the same behavior contracts. Do not create implementations that silently skip validation or side effects unless the interface explicitly allows it.

### Interface Segregation

Keep interfaces small and task-specific. Avoid large service interfaces that combine scanning, planning, execution, logging, and UI concerns.

### Dependency Inversion

ViewModels should depend on service abstractions when there are multiple implementations or when tests need isolation. Do not make ViewModels instantiate concrete file-system services directly.

## Functional Programming Style

Prefer pure functions for planning logic.

A pure planner should:

- Accept input data and options.
- Return a plan or result object.
- Avoid reading or writing files.
- Avoid mutating shared state.
- Avoid showing dialogs or touching WPF controls.

Good pattern:

```text
files + options -> preview plan
```

Bad pattern:

```text
button click -> scan files -> mutate global state -> delete files immediately
```

Use immutable records where practical for data that represents facts, options, or planned operations. Use mutable classes only when WPF binding or incremental UI updates require it.

## File Operation Safety

Every destructive or bulk operation must support preview before execution.

Rules:

- Delete operations should default to moving files to the recycle bin.
- Permanent delete must require explicit UI confirmation if added later.
- Rename operations must detect duplicate target names.
- Rename operations must detect existing target files outside the current plan.
- Rename execution should use temporary names when needed to avoid collisions.
- Execution code must return a structured result with success and error details.

## WPF And MVVM Rules

Views should contain layout and bindings only.

ViewModels may:

- Hold screen state.
- Expose commands.
- Validate user input.
- Convert service results into UI collections.
- Add log messages.

ViewModels should not:

- Directly enumerate files unless delegated to a service.
- Directly delete or rename files unless delegated to an executor.
- Contain natural sorting, frame selection, or rename conflict algorithms.

## UI Rules

This is a utility app. Favor dense, clear, predictable UI over marketing-style layout.

Required UX behavior:

- Users must scan or choose a folder before previewing.
- Users must preview before executing.
- Preview tables must show original filename, action, target filename or delete status, and validation state.
- Errors must be visible in the UI log.

## Git Rules

Use focused commits.

Commit messages should be short and behavior-oriented, for example:

```text
Add frame delete planner tests
Detect duplicate rename targets
Move file execution behind service
```

Do not commit generated build output such as `bin/`, `obj/`, `.vs/`, or packaged executables unless explicitly requested.

## Current Constraints

This project targets:

```text
.NET 8
WPF
Windows
```

The development machine must have the .NET 8 SDK installed to build, run, and test the project.
