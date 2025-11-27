# Distrobox Quick Start

## First Time Setup (5 minutes)

```bash
# 1. Create container (from host, in repo root)
./.distrobox/create-container.sh

# 2. Enter container
distrobox enter dotnetbox

# 3. Run setup script (inside container)
cd ~/path/to/your-project
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
  --name dotnetbox \
  --image registry.fedoraproject.org/fedora-toolbox:40 \
  --yes
```

## Daily Usage

```bash
# Enter container
distrobox enter dotnetbox

# Navigate to project
cd ~/path/to/your-project

# Run your app
dotnet run --project path/to/your.csproj

# Exit when done
exit
```

## File Access

Your entire home directory is available at the same path inside the container!

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
distrobox stop dotnetbox

# Start container
distrobox start dotnetbox

# Delete container
distrobox rm dotnetbox

# Recreate container
./.distrobox/create-container.sh
```

## Need More Help?

See [docs/DISTROBOX_SETUP.md](../docs/DISTROBOX_SETUP.md) for detailed documentation.
