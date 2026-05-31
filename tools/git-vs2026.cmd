@echo off
set "GIT_EXE=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"

if not exist "%GIT_EXE%" (
    echo Visual Studio 2026 bundled Git was not found at: %GIT_EXE% 1>&2
    exit /b 1
)

"%GIT_EXE%" %*
exit /b %ERRORLEVEL%
