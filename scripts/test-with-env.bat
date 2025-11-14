@echo off
REM Set environment variables and run test
set PIGEONPEA_FORCE_KITTY=1
set COLORTERM=truecolor

echo === Environment Set ===
echo PIGEONPEA_FORCE_KITTY = %PIGEONPEA_FORCE_KITTY%
echo COLORTERM = %COLORTERM%
echo.

echo === Running Kitty Test ===
cd /d D:\lunar-snake\personal-work\yokan-projects\pigeon-pea
dotnet run --project dotnet\console-app\PigeonPea.Console.csproj -- --kitty-raw
