# Distrobox Development Environment Setup

This guide explains how to set up and use a distrobox container for .NET GUI development any immutable Linux distribution.

## Why Distrobox?

Distrobox provides a containerized development environment with these benefits:
- ✅ Full GUI application support (shares host display)
- ✅ Access to your entire host home directory
- ✅ Isolated development environment without affecting host system
- ✅ Easy to recreate and share with other developers
- ✅ Works seamlessly on immutable distributions like Bazzite

## Prerequisites

- Bazzite or any Linux distribution with distrobox installed

## Quick Start

### 1. Create the Distrobox Container

From your **host system**, navigate to your project repository and create the container:

```bash
cd ~/path/to/your-project
./.distrobox/create-container.sh
```

Or manually:

```bash
distrobox create \
  --name dotnetbox \
  --image registry.fedoraproject.org/fedora-toolbox:40 \
  --yes
```

This will create a Fedora-based container named `dotnetbox` with:
- Fedora 40 base system
- Access to your entire home directory
- Shared display for GUI applications

### 2. Enter the Container

```bash
distrobox enter dotnetbox
```

Your prompt should change to indicate you're inside the container.

### 3. Run the Setup Script (First Time Only)

Inside the container, navigate to the project and run the setup script:

```bash
cd ~/path/to/your-project
./.distrobox/setup.sh
```

This installs .NET SDK, Avalonia dependencies, and templates.

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run your application
dotnet run --project path/to/your.csproj
```

## File Access

Your entire home directory is automatically available in the container at the same path, so all your files and projects are accessible.

## Container Management

### Start/Stop Container

```bash
# Start container
distrobox enter dotnetbox

# Exit container (from inside)
exit

# Stop container (from host)
distrobox stop dotnetbox

# Start container (from host)
distrobox start dotnetbox
```

### Delete and Recreate Container

If you need to start fresh:

```bash
# Delete container
distrobox rm dotnetbox

# Recreate
cd ~/path/to/your-project
./.distrobox/create-container.sh

# Enter and run setup again
distrobox enter dotnetbox
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
   distrobox enter dotnetbox
   cd ~/path/to/your-project
   ```

2. **Make changes** using your host editor (VS Code, Rider, etc.)
   - The project files are shared between host and container

3. **Build and test** inside the container:
   ```bash
   dotnet build
   dotnet test
   dotnet run --project path/to/your.csproj
   ```

4. **Git operations** can be done on host or in container

5. **Exit** when done:
   ```bash
   exit
   ```

### Using with VS Code

#### Option 1: Install VS Code Inside Container (Recommended)

The easiest way to develop is to install VS Code directly inside the distrobox container. This gives VS Code full access to .NET SDK, X11, and all tools in the container environment.

**First time setup:**

```bash
# Enter the container
distrobox enter dotnetbox

# Run the VSCode installation script
cd ~/path/to/your-project
./.distrobox/install-vscode.sh
```

This script will:
1. Install VS Code inside the container
2. Export it to your host system using `distrobox-export`
3. Create a launcher on your host desktop

**Usage:**

After installation, you can launch VS Code from your host:
- Run `code` from any terminal on your host
- Click the "Visual Studio Code" application in your application menu
- VS Code will run inside the container with full access to all development tools

**Benefits:**
- VS Code runs with .NET SDK and all tools directly available
- IntelliSense and debugging work seamlessly
- No terminal juggling needed
- X11/Wayland forwarding handled automatically

#### Option 2: Use Host VS Code with Container Terminal

You can also use VS Code on the host and build/run in the container:

1. Open the project in VS Code on host
2. Open a terminal in VS Code
3. Run: `distrobox enter dotnetbox`
4. Execute build/run commands in the container terminal

**Drawback:** VS Code extensions won't have direct access to the .NET SDK inside the container.

## Configuration Files

- [.distrobox/create-container.sh](../.distrobox/create-container.sh) - Container creation script
- [.distrobox/setup.sh](../.distrobox/setup.sh) - Post-creation setup script (run inside container)
- [.distrobox/install-vscode.sh](../.distrobox/install-vscode.sh) - VS Code installation and export script
- [.distrobox/distrobox.ini](../.distrobox/distrobox.ini) - Configuration reference (for distrobox 1.9.0+)

## Customization

### Adding More Packages

Edit [.distrobox/create-container.sh](../.distrobox/create-container.sh) to add packages, or install them manually after creation:

```bash
# Inside the container
sudo dnf install YOUR_PACKAGE_HERE
```

### Mounting Additional Directories

If you need to mount directories outside your home directory, edit [.distrobox/create-container.sh](../.distrobox/create-container.sh) and add `--volume` flags:

```bash
distrobox create \
  --name dotnetbox \
  --image registry.fedoraproject.org/fedora-toolbox:40 \
  --volume "/path/on/host:/path/in/container:rw" \
  --yes
```

### Using Different Base Image

Edit [.distrobox/create-container.sh](../.distrobox/create-container.sh) and change the `--image` parameter:

```bash
# For Ubuntu
--image docker.io/library/ubuntu:24.04

# For Arch
--image docker.io/library/archlinux:latest
```

Note: You'll need to adjust package installation commands in [setup.sh](../.distrobox/setup.sh) for different distros.

## Benefits for .NET GUI Development

- **Access to all your files**: Direct access to your entire home directory
- **GUI testing**: Run Avalonia (or other GUI frameworks) natively with full GPU acceleration
- **Isolation**: Don't pollute your host system with development tools
- **Reproducibility**: Share the exact environment with other contributors
- **Easy reset**: Delete and recreate the container anytime
- **Immutable OS friendly**: Perfect for Bazzite, Silverblue, and other immutable distributions

## Getting Help

If you encounter issues:

1. Check this documentation
2. Review the [distrobox documentation](https://distrobox.privatedns.org/)
3. Check your project's issue tracker
