dotnet build "./SPTarkov.Core/SPTarkov.Core.csproj" -c Release -p:OutputType=Library
Copy-Item "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.css" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.css"
Copy-Item "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.js" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.js"
dotnet publish "./SPTarkov.Launcher/SPTarkov.Launcher.csproj" -c Release --self-contained false --framework net9.0 --runtime linux-x64 -p:PublishSingleFile=true
if (Test-Path -Path "./Build") { Remove-Item "./Build" -Recurse -Force }
New-Item -Path "./" -Name "Build" -ItemType "Directory"
Copy-Item "./SPTarkov.Launcher/bin/Release/net9.0/linux-x64/publish/SPTarkov.Launcher" "./Build/SPTarkov.Launcher.Linux"
Copy-Item "./SPTarkov.Core/SPT_Data" "./Build/SPT_Data" -Recurse
