@echo off
setlocal

rem Copy the files from the source to the destination

copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR20.zst Resources\GPSRevitTabVersionR20.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR21.zst Resources\GPSRevitTabVersionR21.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR22.zst Resources\GPSRevitTabVersionR22.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR23.zst Resources\GPSRevitTabVersionR23.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR24.zst Resources\GPSRevitTabVersionR24.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR25.zst Resources\GPSRevitTabVersionR25.zst
copy /Y ..\GPSrvtTab\bin\GPSRevitTabVersionR26.zst Resources\GPSRevitTabVersionR26.zst

rem Build the project in Release mode
dotnet build -c "Release" -p:Platform="Any CPU"

endlocal