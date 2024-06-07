# Launcher

Custom launcher for Escape From Tarkov to start the game in offline mode

**Project**        | **Function**
------------------ | --------------------------------------------
SPT.Build          | Build script
SPT.ByteBanger     | Assembly-CSharp.dll patcher
SPT.Launcher       | Launcher frontend
SPT.Launcher.Base  | Launcher backend

## Privacy
SPT is an open source project. Your commit credentials as author of a commit will be visible by anyone. Please make sure you understand this before submitting a PR.
Feel free to use a "fake" username and email on your commits by using the following commands:
```bash
git config --local user.name "USERNAME"
git config --local user.email "USERNAME@SOMETHING.com"
```

## Requirements

- Escape From Tarkov 30471
- .NET 8 SDK
- Visual Studio Code
- [PowerShell v7](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)

### For UI Development

- Visual Studio Community 2022 (.NET desktop workload)
- Avalonia Visual Studio Extension

## Build
- Ensure `Powershell` is up to date (version 7+)
- Run `Build > Rebuild Solution`
- Build results are stored in `project/Build`

## Server Endpoints
If you just want to mess with the server endpoints, you can use this [postman collection](https://gofile.io/d/kCzmze)