@echo off
REM ============================================================
REM  Defect Classic - one-click build
REM
REM  Usage (double-click = quick build):
REM      build.bat            recompile the .dll only  (seconds)
REM      build.bat publish    also re-export the .pck (after changing images/text)
REM
REM  Close the game first: while it is running the files in the mods folder are
REM  locked and the .pck cannot be replaced (the old `dotnet publish` would just
REM  hang forever in that case - see the notes at the top of pack.py).
REM
REM  Requires: .NET 9 SDK, Python 3.8+, MegaDot 4.5.1 (only for `publish`).
REM  Paths are auto-detected; override with DOTNET_ROOT / MEGADOT_PATH / STS2_PATH.
REM ============================================================
setlocal
cd /d "%~dp0"

set "PY="
where python >nul 2>nul && set "PY=python"
if not defined PY (
    where py >nul 2>nul && set "PY=py"
)
if not defined PY (
    echo [error] Python 3.8+ not found on PATH.
    echo         Install it, or run pack.py manually:  python pack.py [--pck]
    pause
    exit /b 1
)

if /i "%~1"=="publish" (
    "%PY%" pack.py --pck
) else (
    "%PY%" pack.py
)

if errorlevel 1 (
    echo.
    echo [failed] see the messages above.
    pause
    exit /b 1
)

pause
endlocal
