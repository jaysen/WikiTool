#!/bin/bash
# Setup script for WikiTools.Net distrobox environment
# This script installs additional dependencies and configures the environment

set -e

echo "🔧 Setting up WikiTools.Net development environment..."

# Install .NET SDK if not already installed
if ! command -v dotnet &> /dev/null; then
    echo "📦 Installing .NET SDK 9.0..."
    sudo dnf install -y dotnet-sdk-9.0 dotnet-runtime-9.0
else
    echo "✅ .NET SDK already installed: $(dotnet --version)"
fi

# Install system dependencies for Avalonia
echo "📦 Installing GUI dependencies for Avalonia..."
sudo dnf install -y \
    fontconfig \
    liberation-fonts \
    dejavu-sans-fonts \
    dejavu-serif-fonts \
    libICE \
    libSM \
    libX11 \
    libXi \
    libXrandr \
    libXcursor \
    libXext \
    libXrender \
    mesa-libGL

# Install Avalonia templates
echo "📦 Installing Avalonia templates..."
dotnet new install Avalonia.Templates || echo "Avalonia templates may already be installed"

# Verify setup
echo ""
echo "✅ Setup complete!"
echo ""
echo "Environment Information:"
echo "  .NET Version: $(dotnet --version)"
echo "  OS: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)"
echo ""
echo "Next steps:"
echo "  1. Navigate to the project: cd ~/path/to/WikiTools.Net"
echo "  2. Restore dependencies: dotnet restore"
echo "  3. Build the project: dotnet build"
echo "  4. Run the desktop app: dotnet run --project src/WikiTools.Desktop/WikiTools.Desktop.csproj"
echo ""
