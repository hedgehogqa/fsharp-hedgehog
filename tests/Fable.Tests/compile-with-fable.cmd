@echo off
REM WARNING: Must use call because of an issue in npm: https://github.com/npm/npm/issues/2938#issuecomment-10757934
call npm install
call npm run build
