@echo off
setlocal

set "ExePath=%~dp0bin\Debug\net8.0-windows\DevGPT.ExplorerIntegration.exe"

echo Registering Explorer context menu for: %ExePath%

REM Register "Embed files" menu item
reg add "HKCU\Software\Classes\Directory\shell\DevGPT.Embed" /v MUIVerb /t REG_SZ /d "Embed files" /f
reg add "HKCU\Software\Classes\Directory\shell\DevGPT.Embed\command" /ve /t REG_SZ /d "\"%ExePath%\" \"%%1\" --embed" /f

REM Register "Start Chat" menu item
reg add "HKCU\Software\Classes\Directory\shell\DevGPT.StartChat" /v MUIVerb /t REG_SZ /d "Start Chat" /f
reg add "HKCU\Software\Classes\Directory\shell\DevGPT.StartChat\command" /ve /t REG_SZ /d "\"%ExePath%\" \"%%1\" --chat" /f

echo.
echo Done! Right-click on a folder to see the menu items.
echo.
pause
