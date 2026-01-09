@echo off
REM Quick CodeRabbit Runner
REM Just run: scripts\coderabbit.bat

wsl -d Ubuntu bash -c "cd /mnt/c/Users/Niels/Documents/GitHub/Jordnaer && ~/.local/bin/coderabbit --prompt-only --type committed > reviews/review-committed-$(date +%%Y%%m%%d-%%H%%M%%S).txt 2>&1"
