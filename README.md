# Launcher

Custom launcher for Escape From Tarkov to start the game in offline mode

**Project**        | **Function**
------------------ | --------------------------------------------
Aki.Build          | Build script
Aki.ByteBanger     | Assembly-CSharp.dll patcher
Aki.Launcher       | Launcher frontend
Aki.Launcher.Base  | Launcher backend

## Privacy
SPT is an open source project. Your commit credentials as author of a commit will be visible by anyone. Please make sure you understand this before submitting a PR.
Feel free to use a "fake" username and email on your commits by using the following commands:
```bash
git config --local user.name "USERNAME"
git config --local user.email "USERNAME@SOMETHING.com"
```

## Requirements

- Escape From Tarkov 28476
- .NET 6 SDK
- Visual Studio Code
- [PowerShell v7](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)

### For UI Development

- Visual Studio Community 2022 (.NET desktop workload)
- Avalonia Visual Studio Extension

## Build
1. Run `dotnet tool restore` from command line inside project folder
2. Open Launcher.code-workspace in Visual Studio Code.
3. Run the build task: (top toolbar) Terminal -> Run Build Task... (requires running twice on first run)
4. Copy-paste all files inside `Build` into `game root directory`, overwrite when prompted.

## Server Endpoints
If you just want to mess with the server endpoints, you can use this [postman collection](https://gofile.io/d/kCzmze)