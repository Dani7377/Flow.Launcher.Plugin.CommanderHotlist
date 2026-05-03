# Commander Hotlist

A [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) plugin that reads directory hotlists (favorites/bookmarks) from your favorite file manager and lets you quickly navigate to them.

## Supported file managers

- **Total Commander**
- **Double Commander**
- ... more to come!

## Usage

Type the action keyword (default `dc`) in Flow Launcher, then start typing to fuzzy-search your hotlist entries by name or path. Selecting an entry will open that location in the configured file manager.

## Settings

Each supported tool can be individually enabled and configured:

- **Enable/Disable** — toggle whether the tool's hotlist is included in results
- **Executable Path** — path to the file manager's `.exe`
- **Settings File Path** — path to the tool's settings file (by default, `wincmd.ini` for Total Commander, `doublecmd.xml` for Double Commander)
- **Additional Arguments** — extra command-line arguments passed before the target directory