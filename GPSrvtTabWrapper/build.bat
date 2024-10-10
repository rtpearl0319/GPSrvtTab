@echo off
setlocal

rem Copy the files from the source to the destination

copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR20.zst Resources\GPSrvtTabVersionR20.zst
copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR21.zst Resources\GPSrvtTabVersionR21.zst
copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR22.zst Resources\GPSrvtTabVersionR22.zst
copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR23.zst Resources\GPSrvtTabVersionR23.zst
copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR24.zst Resources\GPSrvtTabVersionR24.zst
copy /Y ..\GPSrvtTab\bin\GPSrvtTabVersionR25.zst Resources\GPSrvtTabVersionR25.zst

rem Build the project in Release mode
dotnet build -c "Release" -p:Platform="Any CPU"

endlocal