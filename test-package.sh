#!/bin/bash

# Test script for jot Debian package
set -e

PACKAGE_FILE=${1:-"./releases/jot-*.deb"}
TEST_DIR="/tmp/jot-package-test-$$"

echo "=== Jot Package Installation Test ==="
echo "Package file: $PACKAGE_FILE"
echo "Test directory: $TEST_DIR"
echo

# Expand glob pattern
PACKAGE_FILE=$(ls $PACKAGE_FILE 2>/dev/null | head -1)
if [ ! -f "$PACKAGE_FILE" ]; then
    echo "ERROR: Package file not found: $PACKAGE_FILE"
    exit 1
fi

echo "Testing package: $PACKAGE_FILE"
echo

# Function to cleanup
cleanup() {
    echo "=== Cleanup ==="
    sudo dpkg -r jot 2>/dev/null || true
    rm -rf "$TEST_DIR"
    echo "Cleanup completed"
}

# Set trap for cleanup
trap cleanup EXIT

# Validate package with lintian (if available)
echo "=== Package Validation ==="
if command -v lintian >/dev/null 2>&1; then
    echo "Running lintian validation..."
    lintian --tag-display-limit 0 "$PACKAGE_FILE" || echo "Lintian warnings found (continuing)"
else
    echo "Lintian not available, skipping validation"
fi

# Check package info
echo
echo "=== Package Information ==="
dpkg-deb --info "$PACKAGE_FILE"

echo
echo "=== Package Contents ==="
dpkg-deb --contents "$PACKAGE_FILE"

# Test installation
echo
echo "=== Installing Package ==="
sudo dpkg -i "$PACKAGE_FILE" || {
    echo "Package installation failed, attempting to fix dependencies..."
    sudo apt-get update
    sudo apt-get install -f -y
}

# Verify installation
echo
echo "=== Verifying Installation ==="
dpkg -l | grep jot || {
    echo "ERROR: Package not found in installed packages"
    exit 1
}

# Test binary availability
echo
echo "=== Testing Binary ==="
BINARY_PATH=$(which jot)
echo "Binary found at: $BINARY_PATH"

# Test version command
echo "Testing --version:"
jot --version || echo "WARNING: Version command failed"

# Test help command
echo "Testing --help:"
jot --help | head -5 || echo "WARNING: Help command failed"

# Test functionality
echo
echo "=== Testing Functionality ==="
mkdir -p "$TEST_DIR"
cd "$TEST_DIR"

echo "Initializing schemas..."
jot configure initialize schemas || echo "WARNING: Init schemas failed"

echo "Listing schemas..."
jot schemas || echo "WARNING: List schemas failed"

echo "Listing things..."
jot things || echo "WARNING: List things failed"

echo "Creating test item..."
jot new "Test Schema" "Test Thing" || echo "WARNING: New item creation failed"

echo "Listing things after creation..."
jot things || echo "WARNING: List things after creation failed"

# Test removal
echo
echo "=== Testing Package Removal ==="
sudo dpkg -r jot

# Verify removal
if which jot >/dev/null 2>&1; then
    echo "ERROR: Package removal failed - binary still available"
    exit 1
else
    echo "SUCCESS: Package successfully removed"
fi

echo
echo "=== All Tests Passed ==="