@echo off
setlocal EnableDelayedExpansion

REM CodeRabbit Runner with Authentication and Auto-Setup
REM Just run: scripts\coderabbit.bat

REM Color codes (ANSI escape sequences work in Windows 10+)
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "RESET=[0m"
set "BOLD=[1m"
set "CHECKMARK=✓"
set "CROSS=✗"

echo %BLUE%========================================%RESET%
echo %BOLD%CodeRabbit CLI%RESET%
echo %BLUE%========================================%RESET%
echo.

REM Check if WSL is available
echo %BLUE%Checking prerequisites...%RESET%
wsl --status >nul 2>&1
if errorlevel 1 (
    echo %RED%✗ WSL is not installed or not available.%RESET%
    echo.
    echo Please install WSL with: %YELLOW%wsl --install%RESET%
    echo Then restart your computer and run this script again.
    exit /b 1
)
echo %GREEN%✓ WSL is available%RESET%

REM Check if Ubuntu distribution exists
wsl -d Ubuntu echo "Ubuntu OK" >nul 2>&1
if errorlevel 1 (
    echo %RED%✗ Ubuntu distribution not found in WSL.%RESET%
    echo.
    echo Please install Ubuntu with: %YELLOW%wsl --install -d Ubuntu%RESET%
    echo Then run this script again.
    exit /b 1
)
echo %GREEN%✓ Ubuntu distribution found%RESET%

REM Check if coderabbit is installed, install if not
wsl -d Ubuntu bash -c "test -x ~/.local/bin/coderabbit"
if errorlevel 1 (
    echo %YELLOW%○ CodeRabbit CLI not found - installing...%RESET%
    echo.

    wsl -d Ubuntu bash -c "mkdir -p ~/.local/bin && curl -sSL https://coderabbit.ai/cli/install.sh | bash"

    if errorlevel 1 (
        echo %RED%✗ Failed to install CodeRabbit CLI%RESET%
        exit /b 1
    )
    echo %GREEN%✓ CodeRabbit CLI installed successfully%RESET%
) else (
    echo %GREEN%✓ CodeRabbit CLI is installed%RESET%
)

REM Check if libsecret and gnome-keyring are installed, install if not
wsl -d Ubuntu bash -c "dpkg -l libsecret-1-0 gnome-keyring dbus-x11 2>/dev/null | grep -q '^ii.*libsecret-1-0' && dpkg -l gnome-keyring 2>/dev/null | grep -q '^ii'"
if errorlevel 1 (
    echo %YELLOW%○ Installing authentication dependencies...%RESET%
    echo   (This enables persistent login so you don't need to authenticate every time)
    echo.

    wsl -d Ubuntu bash -c "sudo apt update && sudo apt install -y gnome-keyring libsecret-1-0 dbus-x11"

    if errorlevel 1 (
        echo %RED%✗ Failed to install authentication dependencies%RESET%
        echo %YELLOW%  You may need to authenticate each time you run this script.%RESET%
        echo.
    ) else (
        echo %GREEN%✓ Authentication dependencies installed%RESET%

        REM Setup keyring startup script
        echo %YELLOW%○ Configuring persistent authentication...%RESET%

        REM Convert Windows path to WSL path
        for /f "usebackq tokens=*" %%i in (`wsl -d Ubuntu wslpath -a "%CD%"`) do set "WSL_PATH=%%i"

        REM Copy and install the startup script
        wsl -d Ubuntu bash -c "mkdir -p ~/.local/bin && cp '!WSL_PATH!/scripts/start-keyring.sh' ~/.local/bin/start-keyring.sh && chmod +x ~/.local/bin/start-keyring.sh"

        REM Add to bashrc if not already present
        wsl -d Ubuntu bash -c "if ! grep -q 'start-keyring.sh' ~/.bashrc 2>/dev/null; then echo '' >> ~/.bashrc && echo '# Start gnome-keyring for persistent authentication' >> ~/.bashrc && echo 'if [ -f ~/.local/bin/start-keyring.sh ]; then' >> ~/.bashrc && echo '    source ~/.local/bin/start-keyring.sh' >> ~/.bashrc && echo 'fi' >> ~/.bashrc; fi"

        echo %GREEN%✓ Persistent authentication configured%RESET%
        echo %YELLOW%  Note: You may need to close and reopen your terminal for full effect%RESET%
        echo.
    )
) else (
    echo %GREEN%✓ Authentication dependencies installed%RESET%
)

REM Ensure keyring is started for this session
wsl -d Ubuntu bash -c "if [ -f ~/.local/bin/start-keyring.sh ]; then source ~/.local/bin/start-keyring.sh; fi"

REM Check if reviews directory exists, create if not
if not exist "reviews\" (
    echo %YELLOW%○ Creating reviews directory...%RESET%
    mkdir reviews
    echo %GREEN%✓ Reviews directory created%RESET%
)

echo.
echo %BLUE%Checking authentication status...%RESET%
echo.

REM Check if already authenticated
wsl -d Ubuntu bash -c "~/.local/bin/coderabbit auth status 2>&1" | findstr /C:"✅" >nul 2>&1
set AUTH_STATUS=%errorlevel%

if %AUTH_STATUS% neq 0 (
    echo %YELLOW%========================================%RESET%
    echo %BOLD%Authentication Required%RESET%
    echo %YELLOW%========================================%RESET%
    echo.
    echo A browser will open for authentication:
    echo   1. Complete login at coderabbit.ai
    echo   2. Copy the authentication token
    echo   3. Paste into the WSL popup (Linux penguin icon^)
    echo   4. Press Enter
    echo.

    wsl -d Ubuntu bash -c "~/.local/bin/coderabbit auth login"

    if errorlevel 1 (
        echo.
        echo %RED%========================================%RESET%
        echo %RED%ERROR: Authentication failed!%RESET%
        echo %RED%========================================%RESET%
        exit /b 1
    )
    echo.
    echo %GREEN%✓ Authentication successful!%RESET%
    echo.
) else (
    echo %GREEN%Already authenticated ✓%RESET%
    echo.
)

echo %BLUE%Running CodeRabbit analysis...%RESET%
echo.

REM Convert Windows path to WSL path dynamically
for /f "usebackq tokens=*" %%i in (`wsl -d Ubuntu wslpath -a "%CD%"`) do set "WSL_PATH=%%i"

wsl -d Ubuntu bash -c "cd '%WSL_PATH%' && ~/.local/bin/coderabbit --prompt-only --type committed > reviews/review-committed-$(date +%%Y%%m%%d-%%H%%M%%S).txt 2>&1"

if errorlevel 1 (
    echo.
    echo %RED%========================================%RESET%
    echo %RED%ERROR: CodeRabbit analysis failed!%RESET%
    echo %RED%========================================%RESET%
    echo.
    echo %YELLOW%Check if you have committed changes to analyze.%RESET%
    echo Run: %YELLOW%git status%RESET% to see your repository state.
    exit /b %errorlevel%
)

echo.
echo %GREEN%========================================%RESET%
echo %GREEN%Analysis complete! ✓%RESET%
echo %GREEN%========================================%RESET%
echo.
echo Check the %BLUE%reviews/%RESET% folder for the output file.
echo.
