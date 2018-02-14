@echo off
SETLOCAL
PUSHD %~dp0


.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

if exist paket.lock (
    REM all ok, file exists
) else (
    echo Paket.lock does not exist. use .paket\paket.exe install to create one.
)

.paket\paket.exe restore --group Build
if errorlevel 1 (
  exit /b %errorlevel%
)

SET FSI_PATH=packages\build\FAKE\tools\Fake.exe
"%FSI_PATH%" "build.fsx" Dummy --fsiargs build.fsx --shadowcopyreferences+ %* 
