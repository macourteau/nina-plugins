# Switch Commands

A plugin for N.I.N.A. (Nighttime Imaging 'N' Astronomy) that adds a "Set Switch" sequencer instruction for boolean switches (on/off).

## Motivation

The built-in "Set Switch Value" sequencer instruction passes values through the expression engine, which can fail for boolean switches — the instruction times out even though the Equipment panel's ON/OFF toggles work correctly for the same switches.

This plugin provides a dedicated boolean toggle that directly manipulates the switch, replicating the reliable code path used by the Equipment panel.

## Features

- Simple on/off checkbox control in the sequencer
- Dropdown shows only boolean switches (Min=0, Max=1, Step=1)
- Direct switch manipulation matching the Equipment panel's toggle behavior
- Timeout detection with clear error messages
- Localized validation messages (28 locales)

## Usage

1. Connect your switch hub in the Equipment tab
2. In the Advanced Sequencer, find **Set Switch** under the **Switch** category
3. Select a boolean switch from the dropdown
4. Check the checkbox to turn on, uncheck to turn off
5. Run the sequence — the instruction sets the switch and waits for confirmation

## Localization

The plugin UI label ("Switch") uses N.I.N.A.'s built-in localization. Validation and error messages support 28 locales. The language is automatically selected based on N.I.N.A.'s UI culture setting.

Arabic (ar-SA), Basque (eu-ES), Catalan (ca-ES), Chinese Simplified (zh-CN), Chinese Traditional (zh-HK, zh-TW), Czech (cs-CZ), Danish (da-DK), Dutch (nl-NL), English (default, en-GB), French (fr-CA, fr-FR), Galician (gl-ES), German (de-DE), Greek (el-GR), Hungarian (hu-HU), Italian (it-IT), Japanese (ja-JP), Korean (ko-KR), Norwegian Bokmål (nb-NO), Polish (pl-PL), Portuguese (pt-PT), Russian (ru-RU), Spanish (es-ES), Swedish (sv-SE), Turkish (tr-TR), Ukrainian (uk-UA)

## Requirements

- N.I.N.A. 3.0.0.2017 or later

## Installation

1. Build the project or download the release DLL
2. Copy `SwitchCommands.dll` to `%localappdata%\NINA\Plugins\3.0.0\Switch Commands\`
3. Restart N.I.N.A.
4. The instruction will appear in the Advanced Sequencer under the Switch category

## Building

```bash
cd SwitchCommands/SwitchCommands
dotnet build -c Release
```

The built DLL will be in `bin/Debug/net8.0-windows/` or `bin/Release/net8.0-windows/`.

## License

MPL-2.0
