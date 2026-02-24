# NINA Plugins

## Build Commands

```bash
# UtilityPatterns
dotnet build UtilityPatterns/UtilityPatterns.sln -c Release

# SwitchCommands
dotnet build SwitchCommands/SwitchCommands.sln -c Release
```

## Commit Message Format

Use conventional commits for clear history and auto-generated release notes:

- `feat: <description>` - New features
- `fix: <description>` - Bug fixes
- `docs: <description>` - Documentation changes
- `refactor: <description>` - Code refactoring
- `chore: <description>` - Build/config changes

Examples:
- `feat: Add $$CDATETIME$$ pattern token`
- `fix: Resolve binning value not showing in preview`
- `chore: Update NINA.Plugin to 3.1.0`

## Documentation

Documentation should describe the current state of the code — not when or why something changed. Avoid dated entries, changelogs, or language like "new" or "now" that highlights recent changes. Write as if reading the documentation years from now, when no part of the code is particularly newer than any other.

Update documentation when the changes are relevant (e.g., adding a token, changing behavior), but do not update documentation just because code was touched.

## Releasing

1. Commit changes to main
2. Create and push a version tag:
   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```
3. GitHub Actions builds and creates the release automatically
