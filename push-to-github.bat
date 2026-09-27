@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

set "GHUSER=HZW3210022"
set "REPO=DefectClassic-StS2"
set "URL=https://github.com/%GHUSER%/%REPO%"

echo ============================================================
echo   Push Defect Classic to GitHub
echo ============================================================
echo.
echo   Target: %URL%
echo.
echo   BEFORE YOU RUN THIS, do two things in your browser:
echo.
echo   1) Create the empty repository (leave README / .gitignore /
echo      license UNCHECKED so the push is not rejected):
echo.
echo        https://github.com/new?name=%REPO%%%26visibility=public
echo.
echo   2) Create a Personal Access Token with "repo" scope:
echo.
echo        https://github.com/settings/tokens/new?scopes=repo%%26description=DefectClassic
echo.
echo   This machine has no Git Credential Manager installed, so the
echo   token is the only way to authenticate. It is only used for the
echo   push below and is never written to disk.
echo.
pause

echo.
set /p TOKEN=Paste your token here and press Enter: 
if "%TOKEN%"=="" (
    echo.
    echo [cancelled] no token entered.
    pause
    exit /b 1
)

git branch -M main

echo.
echo Pushing %REPO% ...
git push "https://%GHUSER%:%TOKEN%@github.com/%GHUSER%/%REPO%.git" main

if errorlevel 1 (
    echo.
    echo [failed] Check that:
    echo   - the repository exists and is empty
    echo   - the token has the "repo" scope and has not expired
    pause
    exit /b 1
)

REM keep a clean remote URL without the token in it
git remote remove origin >nul 2>&1
git remote add origin "%URL%.git"
git branch --set-upstream-to=origin/main main >nul 2>&1

echo.
echo ============================================================
echo   Done ->  %URL%
echo ============================================================
echo.
echo   Later, to push more changes:
echo       git add -A ^&^& git commit -m "..." ^&^& git push
echo   (Windows will ask for credentials again; paste the token as the password.)
echo.
pause
endlocal
