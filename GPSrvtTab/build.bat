@echo off
setlocal

echo ""
echo "Building GPSRevitTab for Revit 2020"
dotnet build -c "Debug R20" -p:Platform="Any CPU"

echo ""
echo "Building GPSRevitTab for Revit 2021"
dotnet build -c "Debug R21" -p:Platform="Any CPU"

echo ""
echo "Building GPSRevitTab for Revit 2022"
dotnet build -c "Debug R22" -p:Platform="Any CPU" 

echo ""
echo "Building GPSRevitTab for Revit 2023"
dotnet build -c "Debug R23" -p:Platform="Any CPU"

echo ""
echo "Building GPSRevitTab for Revit 2024"
dotnet build -c "Debug R24" -p:Platform="Any CPU"

echo ""
echo "Building GPSRevitTab for Revit 2025"
dotnet build -c "Debug R25" -p:Platform="Any CPU"

zstd -f -19 -T0 -v "bin\Any CPU\Debug R20\net471-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR20.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Debug R21\net48-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR21.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Debug R22\net48-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR22.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Debug R23\net48-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR23.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Debug R24\net48-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR24.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Debug R25\net8.0-windows\GPSRvtTab.dll" -o "bin\GPSrvtTabVersionR25.zst"

endlocal