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

- Escape From Tarkov 23122
- .NET 6 SDK
- Visual Studio Code

### For UI Development

- Visual Studio Community 2022 (.NET desktop workload)
- Avalonia Visual Studio Extension

## Build
1. Run `dotnet tool restore` from command line inside project folder
2. Open Launcher.code-workspace in Visual Studio Code.
3. Run the build task: (top toolbar) Terminal -> Run Build Task...
4. Copy-paste all files inside `Build` into `game root directory`, overwrite when prompted.
