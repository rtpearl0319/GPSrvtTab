cp ..\GPSrvtTab/bin/GPSRevitTabVersionR20.zst Resources/GPSRevitTabVersionR20.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR21.zst Resources/GPSRevitTabVersionR21.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR22.zst Resources/GPSRevitTabVersionR22.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR23.zst Resources/GPSRevitTabVersionR23.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR24.zst Resources/GPSRevitTabVersionR24.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR25.zst Resources/GPSRevitTabVersionR25.zst
cp ..\GPSrvtTab/bin/GPSRevitTabVersionR26.zst Resources/GPSRevitTabVersionR26.zst

dotnet build -c "Debug" -p:Platform="Any CPU"