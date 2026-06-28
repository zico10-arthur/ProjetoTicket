@echo off
setlocal
set "DSCODE_HOME=%~dp0"
"%DSCODE_HOME%node.exe" "%DSCODE_HOME%dscode.mjs" %*
exit /b %ERRORLEVEL%
