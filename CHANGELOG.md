# Changelog

All notable changes to this project will be documented in this file.

## [0.0.7] - 2025-06-11

### Added

- Release package build system for win-x64, linux-x64, osx-x64, and osx-arm64 targets
- Manual (man) page

### Modified

- CsvHelper removed in favor of Sep for CSV parsing to support AOT / trimmable binary

### Fixed

- When running jot with no default config file, app now creates one with sane defaults

## [0.0.6] - 2025-06-09

### Added

- Setting the value of a `schema` field on a schema shows a chooser if the name, not the GUID, is provided. 
- 'jot' now includes commands to manage import-map field configurations
- 'jot' now supports relative date parsing, such as setting a due date field to 'tomorrow' or 'next Tuesday'
- Home-rooted paths, like "~/Downloads/example.txt" in arguments are now expanded when processed.
- Configuration of storage providers is now made via appsettings.json.
- 'jot' now supports a --filter option for the `things` command for complex filtering expressions.
- 'jot task' suite fo commands to list, create, update, and delete tasks.

### Modified

- Changed built-in system schema `view` so it requires fields `for` and `displayColumns`.
- 'jot' now uses the user's application directory for local storage provider
- 'jot' commands 'things' and 'schemas' now take an optional name filter for searching
- Updated 'jot' to TextPromptWithHistory 1.0.5, which now provides inline prompt editing
- Improve the output of printing schemas and things
- 'jot' commands 'initialize', 'verbose', and 'reindex' are now parented under a 'configure' branch.
- 'jot' has additional hidden command aliases to make the CLI more intuitive
- 'jot' import-maps and import-map command syntax updated
- Invalid field values are now rendered with warning colors in views

### Fixed

- Setting a month+date field now does not complain about an invalid value.

## [0.0.5] - 2025-04-27

### Added

- `pomodoro` (aka `pomo`) command added to jot, whch implements Pomodoro timers

### Modified

- Updated documentation, moved to `docs/` folder under `jot`

## [0.0.4] - 2025-04-13

### Added

- `clear` command added to jot
- Schemas can now have a version plan (see schema version command)

### Modified

- `delete` commands can now take a named argument for ease of use
- Improved saved messages detailing what changed
- Updated README.md
- Updated licensing banners in jot

## [0.0.3] - 2025-04-11

### Added

- 'initialize schemas' command which provides built-in system schemas.

## [0.0.2] - 2025-03-20

### Added

- Added up/down arrow history support to the interactive mode.

## [0.0.1] - 2025-03-18

### Added

- Published Github repository as public to denote this is version 0.0.1
