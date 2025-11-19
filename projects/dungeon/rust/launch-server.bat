@echo off
REM Dungeon Dev Server Launcher for Rio Terminal (Windows Batch)
REM Simplified launcher for quick access

setlocal enabledelayedexpansion

echo 🎮 Dungeon Dev Server Launcher
echo.

REM Check if Rio exists
set "RIO_PATH=D:\lunar-snake\tools\rio\rio.exe"
if not exist "%RIO_PATH%" (
    echo ❌ Rio terminal not found at: %RIO_PATH%
    echo Please install Rio terminal or update path in this script
    pause
    exit /b 1
)

REM Check if Rust/Cargo is available
where cargo >nul 2>&1
if errorlevel 1 (
    echo ❌ Rust/Cargo not found. Please install Rust toolchain.
    echo Visit: https://rustup.rs/
    pause
    exit /b 1
)

REM Change to script directory
cd /d "%~dp0"

echo 🔨 Building Dungeon Dev Server...
cargo check --release --bin dev-tool-server
if errorlevel 1 (
    echo ❌ Build failed. Please check the code for errors.
    pause
    exit /b 1
)

REM Display startup info
echo 📁 Config: rio-config.toml
echo 🌐 Bind: 0.0.0.0:5007
echo 👤 Profile: development
echo 🖥️  Terminal: Rio
echo.

REM Launch Rio terminal with the server
echo 🚀 Launching Rio terminal...
"%RIO_PATH%" --config rio-config.toml --title "Dungeon Dev Server" --profile development --command "cargo run --release --bin dev-tool-server"

if errorlevel 1 (
    echo ❌ Failed to launch Rio terminal
    pause
    exit /b 1
)

echo ✅ Server stopped.
pause
