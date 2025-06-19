# ./src

This directory contains the source code for this solution.

* **Figment.Common** is a class library project that contains base types, such as Schema and Thing, as well as core logic for processing expressions in calculated fields and error handling.  This also holds interfaces for the persistence layer abstraction and any future extensibility provider patterns.
* **Figment.Data.Local** is the persistence layer implementation for local file system storage.  This provider stores Schema and Thing entities in JSON formatted files on the local system.
* **Figment.Data.Memory** is the persistence layer implementation for in-memory storage.  This is not a durable persistence implementation, as memory is erased when the process ends.  It is primarily used for unit tests.
* **Figment.Test** is the test class library project that tests `Figment.Common`, `Figment.Data.Local`, and `Figment.Data.Memory` objects.
* **jot** is a C# console app that exposes functionality of Figment using a command-line interface.  It has both a simple CLI use case when running it from a terminal or from a script, but run with no arguments, it can run in a REPL interactive mode, which provides richer commands and user feedback.
