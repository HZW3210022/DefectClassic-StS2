@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
cd /d "%~dp0"

set "GHUSER=HZW3210022"
set "REPO=DefectClassic-StS2"
set "URL=https://github.com/%GHUSER%/%REPO%.git"

echo ============================================================
echo   Push Defect Classic to GitHub
echo ============================================================
echo.
echo   Target: https://github.com/%GHUSER%/%REPO%
echo.
echo   You need a Personal Access Token that can WRITE to this repo.
echo   Create one here - the "repo" scope is already pre-selected:
echo.
echo     https://github.com/settings/tokens/new?scopes=repo^&description=DefectClassic
echo.
echo   Use that classic-token link rather than a fine-grained token:
echo   a classic token is 40 characters starting with ghp_.
echo   Copy the WHOLE string - a missing first character is the most
echo   common cause of a failed push.
echo.
echo   The token is used for this push only, is never written to disk,
echo   and you should delete it on GitHub afterwards.
echo.
pause

REM --- token: prefer the local token file, fall back to asking ----------------
set "TOKENFILE=%USERPROFILE%\.defect-classic-token"
if exist "%TOKENFILE%" (
    echo   Using the saved token:
    echo     %TOKENFILE%
    echo   ^(edit that file to change it, or delete it to be asked again^)
    set "TOKEN=-"
) else (
    set /p TOKEN=Paste your token and press Enter: 
    if "!TOKEN!"=="" (
        echo.
        echo [cancelled] no token entered.
        pause
        exit /b 1
    )
)

REM --- 0) locate git -----------------------------------------------------------
REM cmd.exe on this machine has no git on PATH, and the standard install
REM locations are empty too. The git that is actually present ships with the
REM editor, so look for a PortableGit under the user profile as well.
REM (The Python helper finds git by itself; this is for the git-push fallback.)
set "GITDIR="
if exist "%LOCALAPPDATA%\Programs\Git\cmd\git.exe" set "GITDIR=%LOCALAPPDATA%\Programs\Git\cmd"
if not defined GITDIR if exist "C:\Program Files\Git\cmd\git.exe" set "GITDIR=C:\Program Files\Git\cmd"
if not defined GITDIR (
    for /d %%v in ("%USERPROFILE%\.workbuddy\binaries\PortableGit\versions\*") do (
        if exist "%%v\cmd\git.exe" set "GITDIR=%%v\cmd"
    )
)
if defined GITDIR set "PATH=!GITDIR!;!PATH!"

git --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo   [note] git was not found on PATH. The API route below does not
    echo          need it, so the push can still work - but if you end up
    echo          seeing 'git is not recognized', that is why.
)

git branch -M main >nul 2>&1

REM --- 1) REST API first --------------------------------------------------------
REM git's HTTPS stack here fails in two different ways (schannel revocation
REM check, or a reset connection) while api.github.com stays solid, so the API
REM is the primary route rather than the fallback.
echo.
echo [1/2] Pushing through the GitHub REST API ...
set "PUSHED=0"

set "PY="
where py >nul 2>&1 && set "PY=py"
if not defined PY ( where python >nul 2>&1 && set "PY=python" )
if not defined PY set "PY=%USERPROFILE%\.workbuddy\binaries\python\versions\3.13.12\python.exe"

if exist "tools\push_via_api.py" (
    %PY% "tools\push_via_api.py" %GHUSER% %REPO% %TOKEN% main
    if not errorlevel 1 set "PUSHED=1"
) else (
    echo       tools\push_via_api.py not found, skipping the API route.
)

if "!PUSHED!"=="1" goto :done

REM --- 2) fall back to git push -------------------------------------------------
if not defined GITDIR (
    echo.
    echo [failed] The API route did not succeed, and git is unavailable,
    echo          so there is no fallback left.
    echo          Most likely the token is the problem - check that you
    echo          copied the whole ghp_... string.
    pause
    exit /b 1
)

echo.
echo [2/2] API route failed - trying git push ...
set "CA=%~dp0tools\cacert.pem"

for /L %%i in (1,1,3) do (
    if "!PUSHED!"=="0" (
        if exist "%CA%" (
            git -c http.sslBackend=openssl -c http.sslCAInfo="%CA%" ^
                push "https://%GHUSER%:%TOKEN%@github.com/%GHUSER%/%REPO%.git" main
        ) else (
            git push "https://%GHUSER%:%TOKEN%@github.com/%GHUSER%/%REPO%.git" main
        )
        if not errorlevel 1 (
            set "PUSHED=1"
        ) else (
            echo       attempt %%i failed, retrying ...
            timeout /t 3 >nul
        )
    )
)

if "!PUSHED!"=="0" (
    echo.
    echo [failed] Neither route worked. Check that:
    echo   - the token can WRITE to this repo and you copied all of it
    echo     (classic: 40 chars from ghp_; fine-grained: "Contents: Read and write")
    echo   - the token has not expired
    echo.
    pause
    exit /b 1
)

:done

REM keep the remote URL clean, without the token in it
git remote remove origin >nul 2>&1
git remote add origin "%URL%" >nul 2>&1
git branch --set-upstream-to=origin/main main >nul 2>&1

echo.
echo ============================================================
echo   Done -^>  https://github.com/%GHUSER%/%REPO%
echo ============================================================
echo.
echo   Remember to delete the token you just used:
echo     https://github.com/settings/tokens
echo.
pause
endlocal
