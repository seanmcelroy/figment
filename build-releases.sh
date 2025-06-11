#!/bin/bash

# Build script for creating multi-platform releases of jot
set -e

VERSION=${1:-$(grep -oP '<Version>\K[^<]+' src/jot/jot.csproj)}
echo "Building jot version: $VERSION"

# Clean previous builds
sudo rm -rf ./releases
mkdir -p ./releases

# Define platforms
declare -a platforms=(
    "win-x64:windows:.exe"
    "linux-x64:linux:"
#    "osx-x64:macos:"
    "osx-arm64:macos-arm:"
)

echo "Building for all platforms..."

for platform_info in "${platforms[@]}"; do
    IFS=':' read -r rid os_name ext <<< "$platform_info"
    echo "Building for $rid ($os_name)..."
    
    # Publish for the platform
    dotnet publish src/jot/jot.csproj \
        --configuration Release \
        --runtime "$rid" \
        --self-contained true \
        --output "./releases/build/$rid" \
        -p:PublishSingleFile=true \
        -p:Version="$VERSION" \
        -p:PublishTrimmed=true
    
    # Create archive
    cd "./releases/build/$rid"
    if [ "$rid" = "win-x64" ]; then
        zip -r "../../jot-$VERSION-$os_name.zip" *
        echo "Created: releases/jot-$VERSION-$os_name.zip"
    else
        tar -czf "../../jot-$VERSION-$os_name.tar.gz" *
        echo "Created: releases/jot-$VERSION-$os_name.tar.gz"
    fi
    cd - > /dev/null
done

# Build Debian package
echo "Building Debian package..."
mkdir -p ./releases/debian-pkg/usr/bin
mkdir -p ./releases/debian-pkg/usr/share/doc/jot
mkdir -p ./releases/debian-pkg/usr/share/man/man1
mkdir -p ./releases/debian-pkg/DEBIAN
chmod 0755 -R ./releases/debian-pkg/usr

# Copy the Linux binary
cp "./releases/build/linux-x64/jot" "./releases/debian-pkg/usr/bin/"
chmod 0755 "./releases/debian-pkg/usr/bin/jot"

# Copy copyright file
cp "./debian/copyright" "./releases/debian-pkg/usr/share/doc/jot/"
chmod 0644 "./releases/debian-pkg/usr/share/doc/jot/copyright"

# Generate Debian changelog from CHANGELOG.md
echo "Generating Debian changelog from CHANGELOG.md..."
./convert-changelog.sh

# Copy and compress changelog
cp "./debian/changelog" "./releases/debian-pkg/usr/share/doc/jot/"
gzip -9n "./releases/debian-pkg/usr/share/doc/jot/changelog"
chmod 0644 "./releases/debian-pkg/usr/share/doc/jot/changelog.gz"

# Copy and compress man page
cp "./debian/jot.1" "./releases/debian-pkg/usr/share/man/man1/"
gzip -9n "./releases/debian-pkg/usr/share/man/man1/jot.1"
chmod 0644 "./releases/debian-pkg/usr/share/man/man1/jot.1.gz"

# Set ownership for dpkg
sudo chown root:root -R ./releases/debian-pkg/usr

# Calculate installed size (in KB)
INSTALLED_SIZE=$(du -sk "./releases/debian-pkg" | cut -f1)

# Create control file
cat > "./releases/debian-pkg/DEBIAN/control" << EOF
Package: jot
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Depends: libc6 (>= 2.31), libgcc-s1, libstdc++6
Installed-Size: $INSTALLED_SIZE
Maintainer: Sean McElroy <me@seanmcelroy.com>
Homepage: https://github.com/seanmcelroy/figment
Description: Command-line personal information manager and task tracker
 A flexible command-line tool for managing personal information with
 customizable schemas, task tracking, and Pomodoro timer functionality.
 .
 Features include:
  * Customizable data schemas for different content types
  * Task management with priorities and due dates  
  * Built-in Pomodoro timer for productivity
  * Import/export capabilities
  * Interactive command-line interface
EOF

# Build the .deb package
dpkg-deb --build "./releases/debian-pkg" "./releases/jot-$VERSION.deb"
echo "Created: releases/jot-$VERSION.deb"

# Clean up build directories
sudo rm -rf "./releases/build"
sudo rm -rf "./releases/debian-pkg"

echo ""
echo "Build complete! Release files:"
ls -la ./releases/

echo ""
echo "To install the Debian package locally:"
echo "sudo dpkg -i ./releases/jot-$VERSION.deb"
echo ""
echo "To test releases:"
echo "./releases/jot-$VERSION-linux.tar.gz - extract and run ./jot"
echo "./releases/jot-$VERSION-windows.zip - extract and run jot.exe"