# NINA Utility Patterns

A plugin for N.I.N.A. (Nighttime Imaging 'N' Astronomy) that provides compact date/time and binning tokens for image file patterns.

## Tokens

| Token | Format | Example | Description |
|-------|--------|---------|-------------|
| `$$CDATE$$` | `yyyyMMdd` | `20260217` | Compact date (local time) |
| `$$CTIME$$` | `HHmmss` | `210122` | Compact time (local time) |
| `$$CDATETIME$$` | `yyyyMMdd_HHmmss` | `20260217_210122` | Compact date+time (local time) |
| `$$CDATEMINUS12$$` | `yyyyMMdd` | `20260217` | Compact date shifted back 12 hours |
| `$$CDATEUTC$$` | `yyyyMMdd` | `20260218` | Compact date (UTC) |
| `$$CTIMEUTC$$` | `HHmmss` | `020122` | Compact time (UTC) |
| `$$CDATETIMEUTC$$` | `yyyyMMdd_HHmmss` | `20260218_020122` | Compact date+time (UTC) |
| `$$CBIN$$` | `N` | `1` or `2` | Binning factor (assumes symmetric binning) |

## Use Cases

### Night Session Date

The `$$CDATEMINUS12$$` token is useful for grouping images from a single night session under the same date. Since astrophotography sessions typically span midnight, this token shifts the date back by 12 hours, so images taken between 6 PM and 6 AM will all use the same date value.

### Compact File Names

The compact date/time tokens produce shorter filenames compared to NINA's built-in tokens:

- `$$DATE$$` produces `2026-02-17` (10 chars)
- `$$CDATE$$` produces `20260217` (8 chars)

### Binning in Filenames

The `$$CBIN$$` token provides just the binning factor number (e.g., `1`, `2`, `3`) rather than the full `1x1` format, useful when you want shorter filenames.

## Requirements

- N.I.N.A. 3.0.0.2017 or later

## Installation

1. Build the project or download the release DLL
2. Copy `NINAUtilityPatterns.dll` to `%localappdata%\NINA\Plugins\3.0.0\NINA Utility Patterns\`
3. Restart NINA
4. The tokens will appear in Options > Imaging > File Patterns

## Building

```bash
cd NINAUtilityPatterns/NINAUtilityPatterns
dotnet build
```

The built DLL will be in `bin/Debug/net8.0-windows/` or `bin/Release/net8.0-windows/`.

## License

MPL-2.0
