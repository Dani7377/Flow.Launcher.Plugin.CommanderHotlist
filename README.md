# Commander Hotlist

A [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) plugin that syncs your directory hotlist (bookmarks) from Total Commander and Double Commander, so you can access your favorite folders directly from Flow Launcher.

![Usage demo](Docs/demo.gif)

## Features

- Sync bookmarks from Total Commander's `wincmd.ini` and Double Commander's `doublecmd.xml` files
- Fuzzy search across both bookmark names and folder paths
- Multiple file managers can be enabled at the same time and the bookmarks from all of them show up together
- Context menu actions: copy folder name or path, open in terminal
- Supported file manages: Total Commander and Double Commander. More to be added in the future!

## Usage

First, you'll need to configure your file manager(s) in the plugin settings. Once configured, type the action keyword (default: `ch`) followed by an optional search term to find a bookmarked folder. For example:

- `ch` — lists all your bookmarks from all configured file managers
- `ch proj` — fuzzy-search for bookmarks matching "proj" in their name or path

Selecting an item opens that folder in the file manager where the bookmark comes from. The plugin also provides several context menu actions you can choose from:

- **Open in \<file manager\>** — opens the folder in a different file manager (if you have more than one configured)
- **Copy folder's name** — copies just the folder name to your clipboard
- **Copy folder's path** — copies the full folder path to your clipboard
- **Open in terminal** — opens the folder in the default terminal

![Context menu](Docs/context_menu.png)

### Search behavior

The plugin fuzzy-matches your search term against both the bookmark name and the folder path, then chooses the best match.

## Settings

![Settings view](Docs/settings.png)

The settings panel lets you configure Total Commander and Double Commander independently.

### Total Commander settings

The following settings are available for TC:

| Setting | Description |
|---|---|
| **Enable** | Allow TC bookmarks to be displayed in the results |
| **Executable Path** | Path to TC executable |
| **Settings INI Path** | Path to your `wincmd.ini` file. For a normal installation this is usually under `C:\Users\<username>\AppData\Roaming\GHISLER`; for a portable installation it's in the TC program folder |
| **Additional Arguments** | Custom arguments passed when opening a folder. Default: `/O /S /T` |

These arguments are optional, but I would recommend to use the following together:

- `/O` — use the already running instance (if any instance is running)
- `/S` — open the folder in the previously active panel
- `/T` — open the folder in a new tab

### Double Commander settings

In a very similar way, the following settings are available for DC:

| Setting | Description |
|---|---|
| **Enable** | Allow DC bookmarks to be displayed in the results |
| **Executable Path** | Path to DC executable |
| **Settings XML Path** | Path to your `doublecmd.xml` file. For a normal installation this is usually under `C:\Users\<username>\AppData\Roaming\doublecmd`; for a portable installation it's in the DC `settings` folder |
| **Additional Arguments** | Custom arguments passed when opening a folder. Default: `-C -T` |

Again, these arguments are optional and here I would recommend to use these ones (note that DC already uses the previously active panel by default, so you won't need a separate argument for that):

- `-C` — use the already running instance (if any instance is running)
- `-T` — open the folder in a new tab

### General settings

This section contains settings that apply to all file managers.

| Setting | Description |
|---|---|
| **Show submenu hierarchy in results** | When enabled, parent submenu names are added at the beginning of the bookmark name (e.g. `Work > Confidential > Word Docs`). |

**Note:** If this setting is enabled, the search results will include the submenu names as well. So with this setting `ON`, searching for `Confidential` will find our `Word Docs` folder from the previous example, but if the option is set to `OFF`, the results will not include this folder.

## License

MIT