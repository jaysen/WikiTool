# Distrobox Development Environment Setup

This guide explains how to set up and use a distrobox container for WikiTools.Net development on Bazzite (or any immutable Linux distribution).

## Why Distrobox?

Distrobox provides a containerized development environment with these benefits:
- ✅ Full GUI application support (shares host display)
- ✅ Access to your host filesystem (including `~/wikis`)
- ✅ Isolated development environment without affecting host system
- ✅ Easy to recreate and share with other developers
- ✅ Works seamlessly on immutable distributions like Bazzite

## Prerequisites

- Bazzite or any Linux distribution with distrobox installed
- Your wiki files should be in `~/wikis`

## Quick Start

### 1. Create the Distrobox Container

From your **host system** (Bazzite), navigate to the WikiTools.Net repository and create the container:

```bash
cd ~/path/to/WikiTools.Net
distrobox create --file .distrobox/distrobox.ini
```

This will create a Fedora-based container named `wikitools-dev` with:
- .NET SDK 9.0
- Required system dependencies for Avalonia UI
- Access to your `~/wikis` directory

### 2. Enter the Container

```bash
distrobox enter wikitools-dev
```

Your prompt should change to indicate you're inside the container.

### 3. Run the Setup Script (First Time Only)

Inside the container, navigate to the project and run the setup script:

```bash
cd ~/path/to/WikiTools.Net
./.distrobox/setup.sh
```

This installs additional dependencies and Avalonia templates.

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the desktop UI
dotnet run --project src/WikiTools.Desktop/WikiTools.Desktop.csproj

# Or run the CLI
dotnet run --project src/WikiTools.CLI/WikiTools.CLI.csproj
```

## Accessing Your Wikis

Your `~/wikis` directory is automatically mounted in the container at the same path. You can access:
- `~/wikis` - Obsidian and WikidPad wikis
- All subdirectories within

The desktop application will be able to browse and convert your wiki files.

## Container Management

### Start/Stop Container

```bash
# Start container
distrobox enter wikitools-dev

# Exit container (from inside)
exit

# Stop container (from host)
distrobox stop wikitools-dev

# Start container (from host)
distrobox start wikitools-dev
```

### Delete and Recreate Container

If you need to start fresh:

```bash
# Delete container
distrobox rm wikitools-dev

# Recreate from config
cd ~/path/to/WikiTools.Net
distrobox create --file .distrobox/distrobox.ini

# Enter and run setup again
distrobox enter wikitools-dev
./.distrobox/setup.sh
```

### List Containers

```bash
distrobox list
```

## Troubleshooting

### GUI Applications Not Working

If the Avalonia app doesn't display:

1. Ensure you're running Wayland or X11 on the host
2. Check display environment variables inside container:
   ```bash
   echo $DISPLAY
   echo $WAYLAND_DISPLAY
   ```

3. Try exporting display manually (inside container):
   ```bash
   export DISPLAY=:0
   ```

### Missing Dependencies

If you encounter missing library errors:

```bash
# Inside container, install additional packages
sudo dnf install <package-name>
```

Common packages for GUI apps:
- `fontconfig`
- `liberation-fonts`
- `libX11`
- `mesa-libGL`

### .NET SDK Issues

Verify .NET installation:

```bash
dotnet --info
```

If .NET is not found, manually install:

```bash
sudo dnf install dotnet-sdk-9.0
```

## Development Workflow

### Typical Session

1. **Start your day**:
   ```bash
   distrobox enter wikitools-dev
   cd ~/path/to/WikiTools.Net
   ```

2. **Make changes** using your host editor (VS Code, Rider, etc.)
   - The project files are shared between host and container

3. **Build and test** inside the container:
   ```bash
   dotnet build
   dotnet test
   dotnet run --project src/WikiTools.Desktop/WikiTools.Desktop.csproj
   ```

4. **Git operations** can be done on host or in container

5. **Exit** when done:
   ```bash
   exit
   ```

### Using with VS Code

You can use VS Code on the host and build/run in the container:

1. Open the project in VS Code on host
2. Open a terminal in VS Code
3. Run: `distrobox enter wikitools-dev`
4. Execute build/run commands in the container terminal

Alternatively, use VS Code's Remote - Containers extension (if available for distrobox).

## Configuration Files

- [.distrobox/distrobox.ini](.distrobox/distrobox.ini) - Container configuration
- [.distrobox/setup.sh](.distrobox/setup.sh) - Post-creation setup script

## Customization

### Adding More Packages

Edit [.distrobox/distrobox.ini](.distrobox/distrobox.ini) and add to `additional_packages`:

```ini
additional_packages="dotnet-sdk-9.0 dotnet-runtime-9.0 fontconfig liberation-fonts git tree YOUR_PACKAGE_HERE"
```

Then recreate the container.

### Mounting Additional Directories

Add more volumes in [.distrobox/distrobox.ini](.distrobox/distrobox.ini):

```ini
volume=/path/on/host:/path/in/container:rw
```

### Using Different Base Image

Change the `image` line in [.distrobox/distrobox.ini](.distrobox/distrobox.ini):

```ini
# For Ubuntu
image=docker.io/library/ubuntu:24.04

# For Arch
image=docker.io/library/archlinux:latest
```

Note: You'll need to adjust package installation commands in [setup.sh](.distrobox/setup.sh) for different distros.

## Benefits for WikiTools Development

- **Test on your actual wikis**: Direct access to `~/wikis`
- **GUI testing**: Run Avalonia UI natively with full GPU acceleration
- **Isolation**: Don't pollute your host system with development tools
- **Reproducibility**: Share the exact environment with other contributors
- **Easy reset**: Delete and recreate the container anytime

## Next Steps

Once your environment is set up:

1. Browse to your wikis: The UI will let you select `~/wikis/your-wiki-folder`
2. Convert between formats: Obsidian ↔ WikidPad
3. Test atomic fixes and changes on real wiki data
4. Contribute to development!

## Getting Help

If you encounter issues:

1. Check this documentation
2. Review the [distrobox documentation](https://distrobox.privatedns.org/)
3. Open an issue on the WikiTools.Net repository
