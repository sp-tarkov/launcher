// ReSharper disable CompareOfFloatsByEqualityOperator
namespace SPTarkov.Core.Forge;

public class ForgeDownloadTask
{
    private string Url { get; init; }
    private HttpClient Client { get; set; }
    private string PathToMod { get; set; } = Path.Combine(Environment.CurrentDirectory, "user", "Launcher", "ModCache");
    private string ModName { get; set; }
    private float TotalToDownload { get; set; }
    public float Progress { get; set; }

    public CancellationTokenSource CancellationToken { get; init; }
    public bool CanShowProgress { get; set; }
    public Exception? Error { get; set; }
    public bool Complete { get; set; }

    public ForgeDownloadTask(
        string modName,
        string url,
        CancellationTokenSource token,
        HttpClient client
    )
    {
        Url = url;
        ModName = modName;
        CancellationToken = token;
        Client = client;
    }

    public async Task<bool> Start()
    {
        var modFilePath = Path.Combine(PathToMod, ModName);
        try
        {
            if (!Directory.Exists(PathToMod))
            {
                Directory.CreateDirectory(PathToMod);
            }

            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }

            using var response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, CancellationToken.Token);
            response.EnsureSuccessStatusCode();

            TotalToDownload = response.Content.Headers.ContentLength ?? -1;
            CanShowProgress = TotalToDownload != -1;

            var contentStream = await response.Content.ReadAsStreamAsync(CancellationToken.Token);
            var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            float totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, CancellationToken.Token);
                totalRead += bytesRead;

                if (CanShowProgress)
                {
                    var now = DateTime.UtcNow;

                    if ((now - lastReportTime).TotalSeconds >= 1 || totalRead == TotalToDownload)
                    {
                        Progress = totalRead / TotalToDownload * 100;
                        lastReportTime = now;
                    }
                }
            }
            await contentStream.FlushAsync();
            contentStream.Close();

            await fileStream.FlushAsync();
            fileStream.Close();

        }
        catch (Exception e)
        {
            Error = e;
            CancellationToken.Cancel();
            return false;
        }

        Complete = true;
        return Complete;
    }

    public async Task<bool> Cancel()
    {
        try
        {
            if (File.Exists(Path.Combine(PathToMod, ModName)))
            {
                File.Delete(Path.Combine(PathToMod, ModName));
            }

            await CancellationToken.CancelAsync();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
