dotnet build "./SPTarkov.Core/SPTarkov.Core.csproj" -c release -p:OutputType=Library
Copy-Item "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.css" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.css"
Copy-Item "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.js" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.js"
dotnet publish "./SPTarkov.Launcher/SPTarkov.Launcher.csproj" -c release --self-contained false --framework net9.0 --runtime win-x64 -p:PublishSingleFile=true
Remove-Item "./Build" -Recurse -Force
mkdir "./Build" -Force
Copy-Item "./SPTarkov.Launcher/bin/Release/net9.0/win-x64/publish/SPTarkov.Launcher.exe" "./Build/SPTarkov.Launcher.exe"
Copy-Item "./SPTarkov.Core/SPT_Data" "./Build/SPT_Data" -Recurse

if ($args.count -eq 1)
{
    $gameDir = [IO.Path]::GetFullPath($args[0])
    Copy-Item "./Build" -Recurse -Force -Destination $gameDir

    Write-Host ""
    Write-Host "Copied build to game folder" -ForegroundColor Green
}