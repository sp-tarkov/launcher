# Launcher

Custom launcher for Escape From Tarkov to start the game in offline mode

**Project**        | **Function**
------------------ | --------------------------------------------
SPTarkov.Core      | Core functionality of the launcher
SPTarkov.Launcher  | Primarily the UI part of the launcher

## Privacy
SPT is an open source project. Your commit credentials as author of a commit will be visible by anyone. Please make sure you understand this before submitting a PR.
Feel free to use a "fake" username and email on your commits by using the following commands:
```bash
git config --local user.name "USERNAME"
git config --local user.email "USERNAME@SOMETHING.com"
```

## Requirements

- Escape From Tarkov *****
- .NET 10 SDK
- Visual Studio or Jetbrains Rider
- webkit2gtk-4.1 dependency for linux users (might be called something slightly different depending on distro :cringe:)
- WebView2 dependency for windows users, this should be on windows by default

## Build
- Open your terminal at `RepositoryFolder/project`
- use this command `dotnet publish "./SPTarkov.Launcher/SPTarkov.Launcher.csproj" -c Release --self-contained false --framework net10.0 --runtime {REPLACE ME WITH RUNTIME} -p:PublishSingleFile=true -p:SptVersion={REPLACE ME WITH SERVER VERSION}
- Two parts need changing to publish
- - `--runtime` is either `win-x64 or linux-x64`
- - `SptVersion` is the servers version number. for example `4.1.0` = `-p:SptVersion=4.1.0`
- this will create a build folder at the project folder