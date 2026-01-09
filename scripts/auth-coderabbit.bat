@echo off
REM CodeRabbit Authentication Helper
REM This opens an interactive WSL session to authenticate CodeRabbit

echo ========================================
echo CodeRabbit CLI Authentication
echo ========================================
echo.
echo This will open an interactive session to authenticate CodeRabbit.
echo A browser window will open for you to log in.
echo.
echo Press any key to continue...
pause > nul
echo.

wsl -d Ubuntu bash -c "~/.local/bin/coderabbit auth login"

echo.
echo ========================================
echo Authentication complete!
echo ========================================
echo.
echo You can now use coderabbit.bat to analyze your code.
echo.
pause
