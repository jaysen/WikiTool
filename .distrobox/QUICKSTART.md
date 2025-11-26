# Distrobox Quick Start

## First Time Setup (5 minutes)

```bash
# 1. Create container (from host, in repo root)
./.distrobox/create-container.sh

# 2. Enter container
distrobox enter wikitools-dev

# 3. Run setup script (inside container)
cd ~/path/to/WikiTools.Net
./.distrobox/setup.sh

# 4. Optional: Install VS Code (recommended)
./.distrobox/install-vscode.sh

# 5. Build project
dotnet restore
dotnet build
```

### Alternative: Manual Container Creation

If the script doesn't work, create manually:

```bash
distrobox create \
  --name wikitools-dev \
  --image registry.fedoraproject.org/fedora-toolbox:40 \
  --volume "$HOME/wikis:$HOME/wikis:rw" \
  --yes
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

## VS Code Integration (Optional)

After running the install script, you can launch VS Code from your host:
- Run `code` from any terminal (it runs inside the container automatically)
- Click "Visual Studio Code" in your application menu
- VS Code will have full access to .NET SDK and all development tools

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
./.distrobox/create-container.sh
```

## Need More Help?

See [docs/DISTROBOX_SETUP.md](../docs/DISTROBOX_SETUP.md) for detailed documentation.
