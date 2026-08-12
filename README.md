# WikiTool

A .NET library, CLI tool, and desktop application for working with various wiki formats.

## Features

- **WikidPad Support**: Read and parse WikidPad `.wiki` files
- **Obsidian Support**: Read and parse Obsidian `.md` files
- **Markdown Wiki Support**: Read and parse using Markdown links
- **Format Conversion**: Convert between wikis - WikidPad to Obsidian and Obsidian to Markdown done.
- **GitHub Pages Output**: The Markdown wiki publishes straight to GitHub Pages
- **CLI Tool**: Command-line interface for easy conversions
- **Desktop GUI**: Cross-platform Avalonia-based desktop application - Converter done

## Requirements

- .NET 10.0 SDK

## Development Setup

### Option 1: Distrobox (Linux)

For GUI development and testing on your actual wikis, use distrobox:

```bash
# Create container
./.distrobox/create-container.sh

# Enter container
distrobox enter dotnetbox

# Run setup script
./.distrobox/setup.sh

# Optional: Install VS Code inside container (recommended)
./.distrobox/install-vscode.sh
```
The VS Code installation script installs VS Code inside the container and exports it to your host, giving you full access to .NET SDK and all development tools

### Option 2: DevContainer

Open this repository in VS Code with the Dev Containers extension, or use GitHub Codespaces.

### Option 3: Local Installation

Install .NET 10.0 SDK on your system directly.

## Building

```bash
dotnet restore
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Usage

### Desktop Application

Run the GUI application:

```bash
dotnet run --project src/WikiTool.Desktop
```

The desktop application provides a visual interface for converting WikidPad wikis to Obsidian format:

- **Browse** for source WikidPad folder
- **Browse** for destination Obsidian folder
- **Options**: Toggle category tag conversion
- **Convert** button to start the conversion
- **Live progress** indicator during conversion
- **Conversion log** showing results and any errors

The GUI is built with Avalonia and runs cross-platform on Windows, Linux, and macOS.

### CLI Tool

Convert a WikidPad wiki to Obsidian format:

```bash
dotnet run --project src/WikiTool.CLI -- convert \
  --from wikidpad \
  --to obsidian \
  --source /path/to/wikidpad \
  --dest /path/to/obsidian
```

Or using short aliases:

```bash
dotnet run --project src/WikiTool.CLI -- convert \
  -f wikidpad \
  -t obsidian \
  -s /path/to/wikidpad \
  -d /path/to/obsidian
```

Convert an Obsidian vault to a GitHub Pages ready Markdown wiki:

```bash
dotnet run --project src/WikiTool.CLI -- convert \
  --from obsidian \
  --to markdown \
  --source /path/to/vault \
  --dest ./docs
```

Supported routes are `wikidpad -> obsidian` and `obsidian -> markdown`, so a WikidPad
wiki can be taken all the way to a published site in two steps.

### WikidPad to Obsidian

The converter handles the following WikidPad syntax:

| WikidPad Format | Obsidian Format | Example |
|----------------|-----------------|---------|
| Headers | Markdown headers | `+ Header` → `# Header` |
| Bare WikiWords | Double brackets | `WikiWord` → `[[WikiWord]]` |
| Single bracket links | Double brackets | `[Link with Spaces]` → `[[Link with Spaces]]` |
| Tags | Hashtags | `[tag:example]` → `#example` |
| Categories (opt-in) | Hashtags | `CategoryName` → `#Name` |
| Attributes | Obsidian attributes | `[author: John]` → `[author:: John]` |
| Aliases | YAML frontmatter | `[alias:Name]` → `aliases:` in frontmatter |
| File extension | Markdown | `.wiki` → `.md` |

**Notes:**
- WikidPad automatically links CamelCase words (WikiWords) without any brackets. Links with spaces or non-CamelCase text use single square brackets `[like this]`. The converter transforms both formats to Obsidian's double-bracket syntax `[[like this]]`.
- Only WikiWords starting with uppercase are converted (e.g., `WikiWord` but not `camelCase` or `iPhone`).
- WikidPad special attributes like `[icon=date]` are preserved unchanged.
- Category conversion is disabled by default. Enable with `--convert-categories` flag or `ConvertCategoryTags = true`.

### Alias Conversion

WikidPad aliases are converted to Obsidian YAML frontmatter:

**Input (WikidPad):**
```
[alias:FirstAlias] [alias:SecondAlias]
+ My Page
Content here
```

**Output (Obsidian):**
```markdown
---
aliases:
  - FirstAlias
  - SecondAlias
---
# My Page
Content here
```

### Obsidian to Markdown Wiki

Converts an Obsidian vault into plain Markdown that publishes to GitHub Pages.

| Obsidian Format | Markdown Wiki Format | Example |
|----------------|----------------------|---------|
| Wikilinks | Relative Markdown links | `[[Beta]]` → `[Beta](beta.md)` |
| Piped links | Markdown link text | `[[Beta\|the notes]]` → `[the notes](beta.md)` |
| Heading links | Anchor links | `[[Beta#Setup]]` → `[Beta > Setup](beta.md#setup)` |
| Same-page headings | Bare anchors | `[[#Risks]]` → `[Risks](#risks)` |
| Inline tags | YAML frontmatter | `#project` → `tags:` in frontmatter |
| Aliases | Jekyll redirects | `aliases:` → `redirect_from:` |
| Filenames | URL-safe slugs | `Alpha Project.md` → `alpha-project.md` |
| Broken links | Bold text | `[[Ghost]]` → `**Ghost**` |

**Notes:**
- Links resolve by page name against the whole vault, exactly as Obsidian does, including
  case-insensitive matches, paths (`[[Projects/Alpha]]`), and aliases. This is why the
  conversion is two-pass: every page must be indexed before any page can be rewritten.
- The original page name is kept as `title:` in frontmatter, so slugging loses nothing.
  A `title:` the author set explicitly takes precedence over the filename.
- Links that resolve to nothing become bold text rather than links that would 404, and
  every one is reported as a warning at the end of the run.
- `[[links]]` and `#tags` inside code blocks and code spans are left alone.
- Block references (`[[Page#^blockid]]`) link to the page, since Markdown has no equivalent.
- `.obsidian/` and other dot-folders are skipped.

**Not converted.** These pass through unchanged, and will render as literal text:

- Embeds and attachments (`![[image.png]]`) - referenced files are not copied
- Callouts (`> [!note]`) - remain plain blockquotes
- Dataview inline fields (`[key:: value]`)

#### Publishing to GitHub Pages

Convert into a `docs/` folder at the root of your repository:

```bash
dotnet run --project src/WikiTool.CLI -- convert -f obsidian -t markdown -s /path/to/vault -d ./docs
```

Then commit, and in the repository go to **Settings → Pages → Deploy from a branch**, and
pick your branch with the **`/docs`** folder.

No CI workflow is needed. GitHub Pages builds Jekyll itself and enables the
`jekyll-relative-links` plugin by default, which rewrites the relative `page.md` links to
`.html` at build time. That is what lets the same files read correctly both when browsing
the repository on github.com and when served from Pages.

Alongside the converted pages the converter writes:

- `_config.yml` - theme and the plugin settings the links depend on
- `indexes/page-index.md` - a listing of every page, grouped by folder
- `indexes/tag-index.md` - a listing of the pages under each tag

Nothing is generated at `index.md`. Jekyll turns that into `index.html`, the site's home
page, so it is left for your own wiki to provide - either a note named `index` or `Home`
that you rename. Link to `indexes/page-index.md` and `indexes/tag-index.md` from it.

The index folder is configurable with `--index-folder`; pass an empty value to write
`page-index.md` and `tag-index.md` at the site root instead. An existing `_config.yml` is
never overwritten without `--force`.

Options for this conversion:

| Option | Effect |
|--------|--------|
| `--no-scaffolding` | Emit only the converted Markdown, no `_config.yml` and no indexes |
| `--index-folder` | Folder for the generated indexes (default `indexes`, empty for the site root) |
| `--keep-inline-tags` | Leave `#tags` in the body instead of hoisting them to frontmatter |
| `--site-title` | Title for the generated site (defaults to the source folder name) |
| `--force` | Overwrite generated site files that already exist |

### Library Usage

```csharp
using WikiTool;
using WikiTool.Converters;

// Read a WikidPad wiki
var wiki = new WikidpadWiki("/path/to/wikidpad");
var pages = wiki.GetAllPages();

// Convert to Obsidian
var converter = new WikidPadToObsidianConverter(
    "/path/to/wikidpad",
    "/path/to/obsidian"
);

// Optional: Enable Category to hashtag conversion (disabled by default)
converter.ConvertCategoryTags = true;

converter.ConvertAll();
```

Converting an Obsidian vault to a GitHub Pages ready Markdown wiki:

```csharp
using WikiTool.Converters;

var converter = new ObsidianToMarkdownConverter(
    "/path/to/vault",
    "./docs"
)
{
    SiteTitle = "My Wiki"
};

converter.ConvertAll();

// Unresolved links, duplicate page names and filename collisions are reported here
foreach (var warning in converter.Warnings)
{
    Console.WriteLine(warning);
}
```

## Project Structure

```
WikiTool/
├── src/
│   ├── WikiTool/                # Core library
│   │   ├── Pages/               # Page implementations
│   │   ├── Wikis/               # Wiki implementations and syntax definitions
│   │   └── Converters/          # Format converters and site generation
│   ├── WikiTool.CLI/            # Command-line interface
│   └── WikiTool.Desktop/        # Avalonia desktop GUI
│       ├── Services/            # UI services (folder picker, etc.)
│       ├── ViewModels/          # MVVM ViewModels
│       └── Views/               # XAML views
├── tests/
│   └── WikiTool.Tests/          # Unit tests
├── docs/                        # Documentation
└── .distrobox/                  # Distrobox development environment
```

## License

See [LICENSE](LICENSE) file for details.
