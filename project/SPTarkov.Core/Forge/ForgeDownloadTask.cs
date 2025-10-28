namespace SPTarkov.Core.Forge;

public class ForgeDownloadTask
{
    private string Url { get; init; }
    private HttpClient Client { get; set; }
    private string PathToMod { get; set; } = Path.Combine(Environment.CurrentDirectory, "Mod");
    private string ModName { get; set; } = "";
    private float? TotalToDownload { get; set; }
    private IProgress<float>? Progress { get; set; }

    public CancellationTokenSource CancellationToken { get; init; }
    public bool CanShowProgress { get; set; }
    public Exception? Error { get; set; }
    public bool Complete { get; set; }

    public ForgeDownloadTask(
        string modName,
        string url,
        CancellationTokenSource token,
        HttpClient client,
        IProgress<float>? progress = null
    )
    {
        Url = url;
        ModName = modName;
        CancellationToken = token;
        Client = client;
        Progress = progress;
        Start();
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
            CanShowProgress = TotalToDownload != -1 && Progress != null;

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
                        var progressValue = totalRead / TotalToDownload * 100;
                        Progress?.Report(progressValue ?? 0f);
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
}
