@echo off
echo Removing DevGPT context menu items...

reg delete "HKCU\Software\Classes\Directory\shell\DevGPT.Embed" /f
reg delete "HKCU\Software\Classes\Directory\shell\DevGPT.StartChat" /f

echo.
echo Done! Context menu items removed.
echo.
pause
