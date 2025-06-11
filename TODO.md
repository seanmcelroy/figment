# Things to build

- [x] Multiple schemas on a thing
- [x] associate schema command
- [x] dissociate schema command
- [x] abstract data layer
- [x] array of strings
- [x] views
- [x] BUG: Setting boolean (active) to 'yes' warns when value unset, but not on subsequent settings
- [x] Calculated fields (age=now() - birthdate)
- [x] BUG: Associating an item to a schema should rebuild that schema's index, or update it (better)
- [x] C/M/A times on entities from filesystem in local provider
- [x] pretty field names
- [x] IMPROVEMENT: Arrow key up in interactive mode to insert previous command
- [x] Improved getting started documentation
- [x] Built-in system schemas
- [x] Pomodoro timer
- [x] tasks
- [ ] Versioned things
- [ ] default schema values
- [ ] Schema inheritence
- [ ] Multiple schema applications
- [ ] calculated views
- [ ] filtered views
- [ ] merge thing command
- [ ] diff command
- [ ] events
- [ ] today
- [ ] calendar
- [ ] travel planner
- [ ] workflows/processes (see github repo bpm)
- [ ] Field formatters (store as RFC 3339 date, accept many, show MMMM d, yyyy)
- [ ] Array of references (owner/owners of teams) properties
- [ ] org chart command ("app"?ee)
- [ ] schema export/import maps / .VCF export/imports
- [ ] remotes and sync between them for various schemas

# Future import improvements

  Data Quality & Validation

  - Pre-import validation report: Show summary of data issues before importing (missing required fields, invalid formats, etc.)
  - Field value statistics: Display min/max/average for numeric fields, value frequency for enums
  - Data type inference: Suggest schema field types based on CSV data patterns

  Import Flexibility

  - Multiple file formats: Add JSON, Excel (.xlsx), XML support beyond CSV
  - Partial imports: Allow importing specific rows/columns with filters
  - Resume capability: Support resuming failed imports from last successful row
  - Chunked processing: Process large files in batches to avoid memory issues

  User Experience

  - Preview mode: Show first N rows of how data will be imported before committing
  - Interactive mapping: Allow users to map CSV columns to schema fields interactively
  - Progress reporting: Real-time progress bar for large imports with ETA
  - Import history: Track and log previous import operations

  Data Management

  - Update existing records: Option to update rather than skip duplicates
  - Backup before import: Create automatic backup of existing data
  - Rollback capability: Ability to undo an import operation
  - Merge strategies: Different approaches for handling conflicts (overwrite, merge, skip)

  Performance & Monitoring

  - Parallel processing: Process multiple rows concurrently for large datasets
  - Memory optimization: Stream processing for very large files
  - Import metrics: Track performance stats (rows/second, memory usage)

  Integration Features

  - Scheduled imports: Support for recurring imports from file paths or URLs
  - External data sources: Import from databases, APIs, cloud storage
  - Transform pipelines: Built-in data transformation during import
