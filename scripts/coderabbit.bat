@echo off
REM CodeRabbit Runner with Authentication
REM Just run: scripts\coderabbit.bat

echo ========================================
echo CodeRabbit CLI
echo ========================================
echo.

REM Check if WSL is available
wsl --status >nul 2>&1
if errorlevel 1 (
    echo ERROR: WSL is not installed or not available.
    echo Please install WSL with: wsl --install
    exit /b 1
)

REM Check if Ubuntu distribution exists
wsl -d Ubuntu echo "Ubuntu OK" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Ubuntu distribution not found in WSL.
    echo Please install Ubuntu: wsl --install -d Ubuntu
    exit /b 1
)

REM Check if coderabbit is installed
wsl -d Ubuntu bash -c "test -x ~/.local/bin/coderabbit"
if errorlevel 1 (
    echo ERROR: CodeRabbit CLI not found at ~/.local/bin/coderabbit
    echo Please install CodeRabbit CLI first.
    exit /b 1
)

REM Check if libsecret is installed (required for secure token storage)
wsl -d Ubuntu bash -c "dpkg -l libsecret-1-0 2>/dev/null | grep -q '^ii'"
if errorlevel 1 (
    echo WARNING: libsecret-1-0 may not be installed.
    echo If authentication fails, install it with:
    echo   wsl -d Ubuntu sudo apt install libsecret-1-0 gnome-keyring
    echo.
)

REM Check if reviews directory exists, create if not
if not exist "reviews\" (
    echo Creating reviews directory...
    mkdir reviews
)

echo Authenticating and running CodeRabbit analysis...
echo.
echo NOTE: Authentication is required each terminal session:
echo   1. A browser will open to coderabbit.ai
echo   2. Copy the authentication token from the website
echo   3. A WSL popup (with Linux penguin icon) will appear
echo   4. Paste the token into the popup and press Enter
echo.

REM Convert Windows path to WSL path dynamically
for /f "usebackq tokens=*" %%i in (`wsl -d Ubuntu wslpath -a "%CD%"`) do set "WSL_PATH=%%i"

wsl -d Ubuntu bash -c "~/.local/bin/coderabbit auth login && cd '%WSL_PATH%' && ~/.local/bin/coderabbit --prompt-only --type committed > reviews/review-committed-$(date +%%Y%%m%%d-%%H%%M%%S).txt 2>&1"

if errorlevel 1 (
    echo.
    echo ========================================
    echo ERROR: CodeRabbit analysis failed!
    echo ========================================
    echo.
    echo Check if you are authenticated and have committed changes.
    exit /b %errorlevel%
)

echo.
echo ========================================
echo Analysis complete!
echo ========================================
echo.
echo Check the reviews/ folder for the output file.
echo.
