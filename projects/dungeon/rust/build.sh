#!/bin/bash

# Build script for dungeon dev tool
# Works around older Rust versions by using compatible dependency versions

set -e

echo "🔨 Building Dungeon Dev Tool..."

# Check Rust version
RUST_VERSION=$(rustc --version | cut -d' ' -f2)
echo "📋 Rust version: $RUST_VERSION"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
cargo clean

# Build with compatibility mode
echo "🏗️  Building with compatibility settings..."
RUSTFLAGS="--cfg tokio_unstable" cargo build --release

# Check if build succeeded
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo "📦 Binary location: target/release/dev-tool"

    # Test the binary
    echo "🧪 Testing binary..."
    ./target/release/dev-tool --help

    echo ""
    echo "🎉 Ready to use!"
    echo "💡 Try: ./target/release/dev-tool --server ws://localhost:5007"
else
    echo "❌ Build failed!"
    echo "💡 Try updating Rust with: rustup update"
    exit 1
fi
