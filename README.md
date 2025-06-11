# Figment

Figment is a personal information manager (PIM).  Figment is implemented as a command-line interpreter (CLI) application named `jot` that lets you jot down your ideas.

The purpose of figment is to let individuals build repositories and graphs of knowledge that help them go about their daily business.  In addition to a customizable data repository with user-defined schemas, built-in functionality aids users in common contact and task and administration duties.

# Features

* Command line interface (CLI) with an operational interactive mode
* Fully customizable data model for both entities and attributes
* [Pomodoro Timers](docs/pomodoro.md) on the command line

# Usage
Compile and run `jot` using the .NET SDK, such as with the command line: `dotnet run --project src/jot/` from the root of this repository.

jot runs in two modes, one that takes command line arguments, like most other CLI tools.  Running jot with no arguments on an interactive console runs it in an interactive REPL-style mode.

When run in interactive mode, use `--help` to see the same commands available to non-interactive mode use and use `ihelp` to see additional commands available in interactive mode.

You can find more [documentation online](https://publish.obsidian.md/seanmcelroy/Projects/Figment/Homepage).

## Building Releases

To build distributable releases for multiple platforms:

```bash
# Build all platforms at once
make build-releases

# Or use the script directly
./build-releases.sh

# Build individual platforms
make build-linux
make build-windows  
make build-macos
```

This creates release packages in the `releases/` directory:
- `jot-VERSION-windows.zip` - Windows executable
- `jot-VERSION-linux.tar.gz` - Linux executable  
- `jot-VERSION-macos.tar.gz` - macOS Intel executable
- `jot-VERSION-macos-arm.tar.gz` - macOS Apple Silicon executable
- `jot-VERSION.deb` - Debian/Ubuntu package

### Installing the Debian Package

```bash
# Install locally
sudo dpkg -i releases/jot-VERSION.deb

# Or use the make target after building
make install-deb
```

# Concepts

## Schemas

There are two major entities in Figment: schemas and things.  Schemas define a set of fields for things of a type, and every thing belongs to at least one schema.  Things can belong to multiple schemas, but they do not have to belong to more than one.

To see an example in practice, you can create a new thing of a new type with the `new` command.  To learn about what commands you can use, you can find details in the CLI help, such as by typing `jot -h` on the command line, or for help specifically with the `new` command, you can type `jot new -h`.

To read more about schemas, check out the [documentation on them](docs/schemas.md) in this repository here.