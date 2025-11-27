#!/bin/bash
# Create .NET GUI development distrobox container using Avalonia
# Compatible with distrobox 1.8.2.1+

set -e

echo "🚀 Creating .NET GUI development container..."
echo ""

# Create the container with all options inline
distrobox create \
  --name dotnetbox \
  --image registry.fedoraproject.org/fedora-toolbox:40 \
  --yes

echo ""
echo "✅ Container 'dotnetbox' created successfully!"
echo ""
echo "Next steps:"
echo "  1. Enter the container: distrobox enter dotnetbox"
echo "  2. Navigate to your project: cd ~/path/to/your-project"
echo "  3. Run setup script: ./.distrobox/setup.sh"
echo ""
