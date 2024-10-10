@echo off
setlocal


cd GPSrvtTab
call build.bat

cd .. 

if %errorlevel%==0 (
    cd GPSrvtTabWrapper
    call build.bat
    cd ..
) else (
    echo First build failed.
)

endlocal