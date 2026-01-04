@echo off
setlocal


cd GPSrvtTab
call build-release.bat

cd .. 

if %errorlevel%==0 (
    cd GPSrvtTabWrapper
    call build-release.bat
    cd ..
) else (
    echo First build failed.
)

endlocal