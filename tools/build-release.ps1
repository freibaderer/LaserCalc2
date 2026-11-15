$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '..\LaserCalc2.sln'
$solution = [System.IO.Path]::GetFullPath($solution)

Write-Host 'Restore und Release-Build ...' -ForegroundColor Cyan
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore ist fehlgeschlagen.' }
dotnet build $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet build ist fehlgeschlagen.' }

Write-Host 'Berechnungsprüfungen ...' -ForegroundColor Cyan
dotnet run --project (Join-Path $PSScriptRoot '..\src\LaserCalc.Verification\LaserCalc.Verification.csproj') --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Mindestens eine Berechnungsprüfung ist fehlgeschlagen.' }

Write-Host 'Self-contained Windows x64 Paket ...' -ForegroundColor Cyan
dotnet publish (Join-Path $PSScriptRoot '..\src\LaserCalc.Desktop\LaserCalc.Desktop.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o (Join-Path $PSScriptRoot '..\artifacts\LaserCalc2')
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish ist fehlgeschlagen.' }
Write-Host 'Fertig. Die Anwendung liegt unter artifacts\LaserCalc2\LaserCalc.exe.' -ForegroundColor Green
