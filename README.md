# NINA Plugins

Plugins for [N.I.N.A.](https://nighttime-imaging.eu/) (Nighttime Imaging 'N' Astronomy).

## Plugins

### NINAUtilityPatterns

Adds compact date/time, binning, and telescope position tokens for image file patterns:

| Token | Format | Description |
|-------|--------|-------------|
| `$$CDATE$$` | `yyyyMMdd` | Compact date (local) |
| `$$CTIME$$` | `HHmmss` | Compact time (local) |
| `$$CDATETIME$$` | `yyyyMMdd_HHmmss` | Compact date+time (local) |
| `$$CDATEMINUS12$$` | `yyyyMMdd` | Date shifted back 12 hours |
| `$$CDATEUTC$$` | `yyyyMMdd` | Compact date (UTC) |
| `$$CTIMEUTC$$` | `HHmmss` | Compact time (UTC) |
| `$$CDATETIMEUTC$$` | `yyyyMMdd_HHmmss` | Compact date+time (UTC) |
| `$$BINX$$` | `N` | Horizontal binning factor |
| `$$BINY$$` | `N` | Vertical binning factor |
| `$$ALT$$` | `N.N` | Telescope altitude in degrees |
| `$$AZ$$` | `N.N` | Telescope azimuth in degrees |
| `$$AIRMASS$$` | `N.N` | Airmass |

## Installation

Download the DLL from [Releases](../../releases) and copy to:
```
%localappdata%\NINA\Plugins\3.0.0\<PluginName>\
```

## Requirements

- N.I.N.A. 3.0.0.2017 or later

## License

MPL-2.0
