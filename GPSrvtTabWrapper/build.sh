cp ..\GPSrvtTab/bin/GPSrvtTabVersionR20.zst Resources/GPSrvtTabVersionR20.zst
cp ..\GPSrvtTab/bin/GPSrvtTabVersionR21.zst Resources/GPSrvtTabVersionR21.zst
cp ..\GPSrvtTab/bin/GPSrvtTabVersionR22.zst Resources/GPSrvtTabVersionR22.zst
cp ..\GPSrvtTab/bin/GPSrvtTabVersionR23.zst Resources/GPSrvtTabVersionR23.zst
cp ..\GPSrvtTab/bin/GPSrvtTabVersionR24.zst Resources/GPSrvtTabVersionR24.zst
cp ..\GPSrvtTab/bin/GPSrvtTabVersionR25.zst Resources/GPSrvtTabVersionR25.zst

dotnet build -c "Release" -p:Platform="Any CPU"