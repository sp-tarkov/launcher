$buildFolder = "..\Build"
$SPTDataFolder = "..\Build\SPT_Data"
$launcherExeFolder = "..\SPT.Launcher\bin\Release\net9.0\win-x64\publish"
$launcherAssetFolder = "..\SPT.Launcher\SPT_Data"
$licenseFile = "..\..\LICENSE.md"

# Delete build folder and contents to ensure it's clean
if (Test-Path "$buildFolder") { Remove-Item -Path "$buildFolder" -Recurse -Force }

# Create build folder and subfolders
$foldersToCreate = @("$buildFolder", "$SPTDataFolder")
foreach ($folder in $foldersToCreate) {
    if (-not (Test-Path "$folder")) { New-Item -Path "$folder" -ItemType Directory }
}

# Move built files to the build folder
Copy-Item -Path "$launcherExeFolder\SPT.Launcher.exe" -Destination "$buildFolder" -Force
Copy-Item -Path "$launcherAssetFolder" -Destination "$buildFolder" -Recurse -Force
# If any new DLLs need to be copied, add here

# Write the contents of the license file to a txt in the build folder
Get-Content "$licenseFile" | Out-File "$buildFolder\LICENSE-Launcher.txt" -Encoding UTF8
