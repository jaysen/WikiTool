# WikiTools.Net

A .NET library and CLI tool for working with various wiki formats, with support for converting between WikidPad and Obsidian.

## Features

- **WikidPad Support**: Read and parse WikidPad `.wiki` files
- **Obsidian Support**: Read and parse Obsidian `.md` files
- **Format Conversion**: Convert WikidPad wikis to Obsidian format
- **CLI Tool**: Command-line interface for easy conversions

## Requirements

- .NET 9.0 SDK

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

The VS Code installation script installs VS Code inside the container and exports it to your host, giving you full access to .NET SDK and all development tools. You can then launch VS Code from your host and it will run inside the container automatically.

See [docs/DISTROBOX_SETUP.md](docs/DISTROBOX_SETUP.md) or [.distrobox/QUICKSTART.md](.distrobox/QUICKSTART.md) for detailed instructions.

### Option 2: DevContainer

Open this repository in VS Code with the Dev Containers extension, or use GitHub Codespaces.

### Option 3: Local Installation

Install .NET 9.0 SDK on your system directly.

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

### CLI Tool

Convert a WikidPad wiki to Obsidian format:

```bash
dotnet run --project src/WikiTools.CLI -- convert \
  --from wikidpad \
  --to obsidian \
  --source /path/to/wikidpad \
  --dest /path/to/obsidian
```

Or using short aliases:

```bash
dotnet run --project src/WikiTools.CLI -- convert \
  -f wikidpad \
  -t obsidian \
  -s /path/to/wikidpad \
  -d /path/to/obsidian
```

### Conversion Details

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

### Library Usage

```csharp
using WikiTools;
using WikiTools.Converters;

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

## Project Structure

```
WikiTools.Net/
├── src/
│   ├── WikiTools/               # Core library
│   │   ├── Pages/               # Page implementations
│   │   ├── Wikis/               # Wiki implementations
│   │   └── Converters/          # Format converters
│   ├── WikiTools.CLI/           # Command-line interface
│   └── WikiTools.Desktop/       # Avalonia desktop GUI (in development)
├── tests/
│   └── WikiTools.Tests/         # Unit tests
├── docs/                        # Documentation
└── .distrobox/                  # Distrobox development environment
```

## License

See [LICENSE](LICENSE) file for details.
