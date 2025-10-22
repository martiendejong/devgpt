@echo off
echo Debugging version detection...
echo.

echo Testing findstr command:
findstr /r "^[    ]*<Version>[0-9]" DevGPT.Classes\DevGPT.Classes.csproj
echo.

echo Extracting version:
for /f "tokens=2 delims=<>" %%a in ('findstr /r "^[    ]*<Version>[0-9]" DevGPT.Classes\DevGPT.Classes.csproj') do (
    echo Token: %%a
)

pause
