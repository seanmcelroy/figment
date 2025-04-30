# Changelog

All notable changes to this project will be documented in this file.

## NEXT

### Added

- 'jot' now includes commands to manage import-map field configurations
- 'jot' now supports relative date parsing, such as setting a due date field to 'tomorrow' or 'next Tuesday'

### Modified

- 'jot' now uses the user's application directory for local storage provider
- 'jot' commands 'things' and 'schemas' now take an optional name filter for searching
- Updated 'jot' to TextPromptWithHistory 1.0.5, which now provides inline prompt editing
- Improve the output of printing schemas and things
- 'jot' commands 'initialize', 'verbose', and 'reindex' are now parented under a 'configure' branch.
- 'jot' has additional hidden command aliases to make the CLI more intuitive
- 'jot' import-maps and import-map command syntax updated

### Fixed

- Setting a month+date field now does not complain about an invalid value.

## 0.0.5

### Added

- 'pomo' command added to jot, whch implements Pomodoro timers

## 0.0.4

### Added

- 'clear' command added to jot
- Schemas can now have a version plan (see schema version command)

### Modified

- 'delete' command can now take a named argument for ease of use
- Improved saved messages detailing what changed
- Updated README.md
- Updated licensing banners in jot

## 0.0.3

### Added

- 'initialize schemas' command which provides built-in system schemas.

## 0.0.2

### Added

- Added up/down arrow history support to the interactive mode.

## 0.0.1

### Added

- Published Github repository as public to denote this is version 0.0.1