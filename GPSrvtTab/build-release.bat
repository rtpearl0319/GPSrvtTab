@echo off
setlocal

dotnet build -c "Release R20" -p:Platform="Any CPU"
dotnet build -c "Release R21" -p:Platform="Any CPU"
dotnet build -c "Release R22" -p:Platform="Any CPU"
dotnet build -c "Release R23" -p:Platform="Any CPU"
dotnet build -c "Release R24" -p:Platform="Any CPU"
dotnet build -c "Release R25" -p:Platform="Any CPU"
dotnet build -c "Release R26" -p:Platform="Any CPU"

zstd -f -19 -T0 -v "bin\Any CPU\Release R20\net471-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR20.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R21\net48-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR21.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R22\net48-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR22.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R23\net48-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR23.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R24\net48-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR24.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R25\net8.0-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR25.zst"
zstd -f -19 -T0 -v "bin\Any CPU\Release R26\net8.0-windows\GPSRvtTab.dll" -o "bin\GPSRevitTabVersionR26.zst"

endlocal