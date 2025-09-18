dotnet build "./SPTarkov.Core/SPTarkov.Core.csproj" -c release -p:OutputType=Library
copy "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.css" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.css"
copy "./SPTarkov.Core/bin/Release/net9.0/MudBlazor.min.js" "./SPTarkov.Launcher/wwwroot/MudBlazor.min.js"
dotnet publish "./SPTarkov.Launcher/SPTarkov.Launcher.csproj" -c release --self-contained false --framework net9.0 --runtime win-x64 -p:PublishSingleFile=true
mkdir "./Build" -Force
copy "./SPTarkov.Launcher/bin/Release/net9.0/win-x64/publish/SPTarkov.Launcher.exe" "./Build/SPTarkov.Launcher.exe"