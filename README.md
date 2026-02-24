# NINA Plugins

Plugins for [N.I.N.A.](https://nighttime-imaging.eu/) (Nighttime Imaging 'N' Astronomy).

## [SwitchCommands](SwitchCommands/)

Adds a "Set Switch" sequencer instruction for boolean switches (on/off). The built-in "Set Switch Value" instruction can fail for boolean switches due to its expression engine code path — this plugin directly manipulates the switch, replicating the Equipment panel's reliable toggle behavior.

- Simple on/off checkbox control
- Dropdown shows only boolean switches (Min=0, Max=1, Step=1)
- Timeout detection with clear error messages

## [UtilityPatterns](UtilityPatterns/)

Adds compact date/time, binning, and telescope position tokens for image file patterns:

| Token | Format | Description |
|-------|--------|-------------|
| `$$AIRMASS$$` | `N.N` | Airmass |
| `$$ALT$$` | `N.N` | Telescope altitude in degrees |
| `$$AZ$$` | `N.N` | Telescope azimuth in degrees |
| `$$BINX$$` | `N` | Horizontal binning factor |
| `$$BINY$$` | `N` | Vertical binning factor |
| `$$CDATE$$` | `yyyyMMdd` | Compact date (local) |
| `$$CDATEMINUS12$$` | `yyyyMMdd` | Date shifted back 12 hours |
| `$$CDATETIME$$` | `yyyyMMdd_HHmmss` | Compact date+time (local) |
| `$$CDATETIMEUTC$$` | `yyyyMMdd_HHmmss` | Compact date+time (UTC) |
| `$$CDATEUTC$$` | `yyyyMMdd` | Compact date (UTC) |
| `$$CTIME$$` | `HHmmss` | Compact time (local) |
| `$$CTIMEUTC$$` | `HHmmss` | Compact time (UTC) |

---

## Installation

Download `NINAPlugins.zip` from [Releases](../../releases) and extract to:
```
%localappdata%\NINA\Plugins\3.0.0\
```

The zip contains each plugin in its own subdirectory, ready to use. Individual DLLs are also available as separate downloads.

## Requirements

- N.I.N.A. 3.0.0.2017 or later

## License

MPL-2.0
