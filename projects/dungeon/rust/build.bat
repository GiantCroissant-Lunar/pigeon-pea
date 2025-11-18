@echo off
echo 🔨 Building Dungeon Dev Tool...

REM Check Rust version
for /f "tokens=2" %%i in ('rustc --version') do set RUST_VERSION=%%i
echo 📋 Rust version: %RUST_VERSION%

REM Clean previous builds
echo 🧹 Cleaning previous builds...
cargo clean

REM Build with compatibility mode
echo 🏗️  Building with compatibility settings...
set RUSTFLAGS=--cfg tokio_unstable
cargo build --release

REM Check if build succeeded
if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
    echo 📦 Binary location: target\release\dev-tool.exe

    REM Test the binary
    echo 🧪 Testing binary...
    target\release\dev-tool.exe --help

    echo.
    echo 🎉 Ready to use!
    echo 💡 Try: target\release\dev-tool.exe --server ws://localhost:5007
) else (
    echo ❌ Build failed!
    echo 💡 Try updating Rust with: rustup update
    exit /b 1
)
