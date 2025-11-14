@echo off
echo === Terminal Environment Check ===
echo.

echo TERM = %TERM%
echo TERM_PROGRAM = %TERM_PROGRAM%
echo COLORTERM = %COLORTERM%
echo PIGEONPEA_FORCE_KITTY = %PIGEONPEA_FORCE_KITTY%
echo WEZTERM_CONFIG_FILE = %WEZTERM_CONFIG_FILE%
echo.

echo === WezTerm Version ===
wezterm --version
echo.

echo === Test Simple ANSI Color ===
echo [31mRed Text[0m (if you see colored "Red Text", ANSI works)
echo.
pause
