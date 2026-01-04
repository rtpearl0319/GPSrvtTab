dotnet build  -c "Debug R20" -p:Platform="Any CPU"
dotnet build  -c "Debug R21" -p:Platform="Any CPU"
dotnet build  -c "Debug R22" -p:Platform="Any CPU"
dotnet build  -c "Debug R23" -p:Platform="Any CPU"
dotnet build  -c "Debug R24" -p:Platform="Any CPU"
dotnet build  -c "Debug R25" -p:Platform="Any CPU"
dotnet build  -c "Debug R26" -p:Platform="Any CPU"

zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R20/net471/GPSrvtTab.dll -o bin/GPSRevitTabVersionR20.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R21/net48/GPSrvtTab.dll -o bin/GPSRevitTabVersionR21.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R22/net48/GPSrvtTab.dll -o bin/GPSRevitTabVersionR22.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R23/net48/GPSrvtTab.dll -o bin/GPSRevitTabVersionR23.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R24/net48/GPSrvtTab.dll -o bin/GPSRevitTabVersionR24.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R25/net8.0/GPSrvtTab.dll -o bin/GPSRevitTabVersionR25.zst
zstd -f -19 -T0 -v bin/Any\ CPU/Debug\ R26/net8.0/GPSrvtTab.dll -o bin/GPSRevitTabVersionR26.zst