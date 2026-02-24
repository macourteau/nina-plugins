# Utility Patterns

A plugin for N.I.N.A. (Nighttime Imaging 'N' Astronomy) that provides compact date/time, binning, and telescope position tokens for image file patterns.

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
| `$$BINX$$` | `N` | `1` or `2` | Horizontal binning factor |
| `$$BINY$$` | `N` | `1` or `2` | Vertical binning factor |
| `$$ALT$$` | `N.N` | `45.2` or `NA` | Telescope altitude in degrees |
| `$$AZ$$` | `N.N` | `180.5` or `NA` | Telescope azimuth in degrees |
| `$$AIRMASS$$` | `N.N` | `1.4` or `NA` | Airmass |

## Use Cases

### Night Session Date

The `$$CDATEMINUS12$$` token is useful for grouping images from a single night session under the same date. Since astrophotography sessions typically span midnight, this token shifts the date back by 12 hours, so images taken between 6 PM and 6 AM will all use the same date value.

### Compact File Names

The compact date/time tokens produce shorter filenames compared to N.I.N.A.'s built-in tokens:

- `$$DATE$$` produces `2026-02-17` (10 chars)
- `$$CDATE$$` produces `20260217` (8 chars)

### Binning in Filenames

The `$$BINX$$` and `$$BINY$$` tokens provide just the binning factor number (e.g., `1`, `2`, `3`), useful when you want shorter filenames or need to distinguish horizontal and vertical binning.

### Telescope Position in Filenames

The `$$ALT$$`, `$$AZ$$`, and `$$AIRMASS$$` tokens capture the telescope's position at the time each image is saved. This is useful for:

- Sorting images by altitude to identify frames taken through more atmosphere
- Filtering by airmass for quality assessment
- Organizing images by sky region using azimuth

When the telescope is not connected, these tokens resolve to `NA`.

## Localization

The plugin supports the following languages:

- English (default)
- French (Canada) - fr-CA
- French (France) - fr-FR

The language is automatically selected based on N.I.N.A.'s UI culture setting.

## Requirements

- N.I.N.A. 3.0.0.2017 or later

## Installation

1. Build the project or download the release DLL
2. Copy `UtilityPatterns.dll` to `%localappdata%\NINA\Plugins\3.0.0\Utility Patterns\`
3. Restart N.I.N.A.
4. The tokens will appear in Options > Imaging > File Patterns

## Building

```bash
cd UtilityPatterns/UtilityPatterns
dotnet build -c Release
```

The built DLL will be in `bin/Debug/net8.0-windows/` or `bin/Release/net8.0-windows/`.

## License

MPL-2.0
