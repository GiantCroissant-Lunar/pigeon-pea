@echo off
REM Launch a fresh WezTerm instance from the Feb 2024 version
REM This ensures we're using the right version with Kitty support

set "WEZTERM_EXE=D:\lunar-snake\tools\WezTerm-windows-20240203-110809-5046fc22\wezterm-gui.exe"
set "WEZTERM_CONFIG=D:\lunar-snake\tools\WezTerm-windows-20240203-110809-5046fc22\wezterm.lua"
set "PROJECT_DIR=D:\lunar-snake\personal-work\yokan-projects\pigeon-pea"

echo Launching WezTerm (Feb 2024 version)...
echo Config: %WEZTERM_CONFIG%
echo.
echo After it opens, you can:
echo   1. Run: scripts\check-terminal.bat
echo   2. Run: scripts\test-with-env.bat
echo   3. Generate test image: task images:create-test
echo   4. Test imgcat: wezterm imgcat tests\kitty\assets\test_red.png
echo.

set "WEZTERM_CONFIG_FILE=%WEZTERM_CONFIG%"
start "" "%WEZTERM_EXE%" start --cwd "%PROJECT_DIR%"
