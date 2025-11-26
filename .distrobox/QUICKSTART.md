# Distrobox Quick Start

## First Time Setup (5 minutes)

```bash
# 1. Create container (from host, in repo root)
distrobox create --file .distrobox/distrobox.ini

# 2. Enter container
distrobox enter wikitools-dev

# 3. Run setup script (inside container)
cd ~/path/to/WikiTools.Net
./.distrobox/setup.sh

# 4. Build project
dotnet restore
dotnet build
```

## Daily Usage

```bash
# Enter container
distrobox enter wikitools-dev

# Navigate to project
cd ~/path/to/WikiTools.Net

# Run desktop app
dotnet run --project src/WikiTools.Desktop/WikiTools.Desktop.csproj

# Or run CLI
dotnet run --project src/WikiTools.CLI/WikiTools.CLI.csproj

# Exit when done
exit
```

## Your Wikis

Your `~/wikis` directory is available at the same path inside the container!

## Common Commands

```bash
# List containers
distrobox list

# Stop container
distrobox stop wikitools-dev

# Start container
distrobox start wikitools-dev

# Delete container
distrobox rm wikitools-dev

# Recreate container
distrobox create --file .distrobox/distrobox.ini
```

## Need More Help?

See [docs/DISTROBOX_SETUP.md](../docs/DISTROBOX_SETUP.md) for detailed documentation.
