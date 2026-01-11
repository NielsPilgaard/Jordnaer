@echo off
REM CodeRabbit Runner with Authentication
REM Just run: scripts\coderabbit.bat

echo ========================================
echo CodeRabbit CLI
echo ========================================
echo.
echo Authenticating and running CodeRabbit analysis...
echo A browser window may open for login if needed.
echo.

wsl -d Ubuntu bash -c "~/.local/bin/coderabbit auth login && cd /mnt/c/Users/Niels/Documents/GitHub/Jordnaer && ~/.local/bin/coderabbit --prompt-only --type committed > reviews/review-committed-$(date +%%Y%%m%%d-%%H%%M%%S).txt 2>&1"

echo.
echo ========================================
echo Analysis complete!
echo ========================================
echo.
echo Check the reviews/ folder for the output file.
echo.
