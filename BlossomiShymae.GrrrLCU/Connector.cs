using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlossomiShymae.GrrrLCU
{
    /// <summary>
    /// Connector to exchange requests with the League Client.
    /// </summary>
    public static class Connector
    {
        internal static HttpClient HttpClient { get; } = new(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        internal static JsonSerializerOptions JsonSerializerOptions { get; } = new()
        {
            PropertyNameCaseInsensitive = true
        };

        internal static ProcessInfo GetProcessInfo()
        {
            ProcessInfo? processInfo = null;

            foreach (var process in Process.GetProcesses())
            {
                switch(process.ProcessName)
                {
                    case "LeagueClientUx":
                        processInfo = new ProcessInfo(process);
                        break;
                    default:
                        break;
                }

                if (processInfo != null) break;
            }

            return processInfo ?? throw new InvalidOperationException("Failed to find LCUx process.");
        }

        internal static Uri GetLeagueClientUri(int appPort, string path)
        {
            return new Uri($"https://127.0.0.1:{appPort}{path}");
        }

        /// <summary>
        /// Set the timeout for the internal HttpClient.
        /// </summary>
        /// <param name="timeSpan"></param>
        public static void SetTimeout(TimeSpan timeSpan)
        {
            HttpClient.Timeout = timeSpan;
        }

        /// <summary>
        /// Send a request to the League Client.
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string path, CancellationToken cancellationToken = default)
        {
            var processInfo = GetProcessInfo();
            var riotAuthentication = new RiotAuthentication(processInfo.RemotingAuthToken);

            var request = new HttpRequestMessage(httpMethod, GetLeagueClientUri(processInfo.AppPort, path));
            request.Headers.Authorization = riotAuthentication.ToAuthenticationHeaderValue();
                        
            var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Send a GET request to the League Client.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(HttpMethod.Get, path, cancellationToken).ConfigureAwait(false);
            
            return response;
        }

        /// <summary>
        /// Send a GET request to the League Client for deserialized JSON data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T?> GetFromJsonAsync<T>(string path, JsonSerializerOptions? options = default, CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(path, cancellationToken).ConfigureAwait(false);
            
            var data = await response.Content.ReadFromJsonAsync<T>(options ?? JsonSerializerOptions, cancellationToken).ConfigureAwait(false);

            return data;
        }
    }
}