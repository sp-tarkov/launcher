// ReSharper disable CompareOfFloatsByEqualityOperator

using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

public class Mod
{
    private string Url { get; }
    private HttpClient Client { get; }
    private string PathToModInCache { get; } = Path.Combine(Environment.CurrentDirectory, Paths.ModCache);
    private string ModName { get; }
    private float TotalToDownload { get; set; }
    public float Progress { get; private set; }

    public CancellationTokenSource CancellationToken { get; }
    public bool CanShowProgress { get; set; }
    public Exception? Error { get; set; }
    public bool Complete { get; private set; }

    public Mod(
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

    public async Task<bool> StartModDownload()
    {
        var modFilePath = Path.Combine(PathToModInCache, ModName);
        try
        {
            if (!Directory.Exists(PathToModInCache))
            {
                Directory.CreateDirectory(PathToModInCache);
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

    public async Task<bool> CancelModDownload()
    {
        try
        {
            await CancellationToken.CancelAsync();

            if (File.Exists(Path.Combine(PathToModInCache, ModName)))
            {
                File.Delete(Path.Combine(PathToModInCache, ModName));
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateMod()
    {
        return true;
    }

    public async Task<bool> RemoveMod()
    {
        return true;
    }
}
