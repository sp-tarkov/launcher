/* Request.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SPT.Launcher.MiniCommon
{
    public class Request(string session, string remoteEndPoint)
    {
        public string Session = session;
        public string RemoteEndPoint = remoteEndPoint;

        private static readonly HttpClientHandler httpClientHandler = new()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            UseProxy = false,
            UseCookies = false
        };

        private static readonly HttpClient _httpClient = new(httpClientHandler);

        public async Task<HttpResponseMessage> Send(string url, string method = "GET", string data = null, bool compress = true, CancellationToken cts = default)
        {
            // set session headers
            var requestUri = new Uri(RemoteEndPoint + url);
            using var requestMessage = new HttpRequestMessage(new HttpMethod(method), requestUri);
            requestMessage.Headers.ExpectContinue = true;

            if (!string.IsNullOrWhiteSpace(Session))
            {
                requestMessage.Headers.Add("Cookie", $"PHPSESSID={Session}");
                requestMessage.Headers.Add("SessionId", Session);
            }

            requestMessage.Headers.Add("Accept-Encoding", "deflate");
            requestMessage.Method = new(method);

            if (method != "GET" && !string.IsNullOrWhiteSpace(data))
            {
                // set request body
                var bytes = compress ? await CompressZlibAsync(data, cts) : Encoding.UTF8.GetBytes(data);
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                if (compress)
                {
                    content.Headers.ContentEncoding.Add("deflate");
                }

                requestMessage.Content = content;
            }

            // get response stream
            try
            {
                var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts);

                response.EnsureSuccessStatusCode();

                return response;
            }
            catch (Exception)
            {
                // Not sure why this was a unityengine debug logger. Possibly used by another module?
            }

            return null;
        }

        public async Task<string> GetJsonAsync(string url, bool compress = true, CancellationToken cancellationToken = default)
        {
            using var response = await Send(url, "GET", null, compress, cancellationToken);
           await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return await DecompressZlibAsync(ms.ToArray(), cancellationToken);
        }

        public async Task<string> PostJsonAsync(string url, string data, bool compress = true, CancellationToken cancellationToken = default)
        {
            using var response =  await Send(url, "POST", data, compress, cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return await DecompressZlibAsync(ms.ToArray(), cancellationToken);
        }

        private async Task<byte[]> CompressZlibAsync(string input, CancellationToken cancellationToken)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);

            await using var ms = new MemoryStream();
            await using (var stream = new ZLibStream(ms, CompressionLevel.Optimal))
            {
                await stream.WriteAsync(inputBytes, cancellationToken);
            }

            return ms.ToArray();
        }

        private async Task<string> DecompressZlibAsync(byte[] compressedBytes, CancellationToken cancellationToken)
        {
            await using var ms = new MemoryStream(compressedBytes);
            await using var deflateStream = new ZLibStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream, Encoding.UTF8);

            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
