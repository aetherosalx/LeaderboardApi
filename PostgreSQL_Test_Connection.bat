@echo off
setlocal

:: === CONFIGURATION ===
set PSQL_PATH="C:\Program Files\PostgreSQL\17\bin\psql.exe"
set PG_HOST=localhost
set PG_PORT=5432
set PG_USER=postgres
set PG_DB=Leaderboard

:: Prompt for password securely
set /p PG_PASSWORD=Enter PostgreSQL password: 

:: === TEST CONNECTION ===
echo.
echo Testing connection to %PG_DB% on %PG_HOST%:%PG_PORT% as %PG_USER%
echo.

:: Call psql
%PSQL_PATH% -h %PG_HOST% -p %PG_PORT% -U %PG_USER% -d %PG_DB% -c "SELECT version();"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Connection successful!
) else (
    echo.
    echo ❌ Connection failed!
)

endlocal
pause
