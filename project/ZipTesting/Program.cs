using SPTarkov.Core.SevenZip;

namespace ZipTesting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var Test = new WindowsSevenZip();
        Test.PathToSevenZip = @"C:\Repos\launcher\project\SPTarkov.Core\SPT_Data\Launcher\Dependency";
        // var pathToZip = @"C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\fika.ghostfenixx.svm";
        var pathToZip = @"C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\xyz.drakia.waypoints";
        // var pathToZip = @"C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\me.sol.sain";
        var test2 = await Test.ExtractToDirectoryAsync(pathToZip, @"C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\test");
        Console.WriteLine("Hopefully done");
    }
}
