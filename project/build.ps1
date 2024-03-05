$buildFolder = "..\Build"
$akiDataFolder = "..\Build\Aki_Data"
$launcherExeFolder = "..\Aki.Launcher\bin\Release\net6.0\win-x64\publish"
$launcherAssetFolder = "..\Aki.Launcher\Aki_Data"
$licenseFile = "..\..\LICENSE.md"

# Delete build folder and contents to ensure it's clean
if (Test-Path $buildFolder) { Remove-Item -Path $buildFolder -Recurse -Force }

# Create build folder and subfolders
$foldersToCreate = @($buildFolder, $akiDataFolder)
foreach ($folder in $foldersToCreate) {
    if (-not (Test-Path $folder)) { New-Item -Path $folder -ItemType Directory }
}

# Move built files to the build folder
Copy-Item -Path "$launcherExeFolder\Aki.Launcher.exe" -Destination $buildFolder
Copy-Item -Path $launcherAssetFolder -Destination "$buildFolder\Aki_Data" -Recurse
# (If any new DLLs need to be copied, add here)

# Write the contents of the license file to a txt in the build folder.
Get-Content $licenseFile | Out-File "$buildFolder\LICENSE-Launcher.txt"
