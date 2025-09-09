using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using ModedGame.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModedGame.Services
{
    public class MCAuthenticationService
    {
        private const string ClientId = "47f04401-8b21-48b3-ae4b-20e7187c2341";

        private readonly IPublicClientApplication _msalClient;
        private readonly HttpClient _httpClient;

        private MCAuthenticationService(IPublicClientApplication msalClient)
        {
            _msalClient = msalClient;
            _httpClient = new HttpClient();
        }

        // This is the new, simplified factory method
        public static async Task<MCAuthenticationService> CreateAsync()
        {
            var cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyMinecraftLauncher");
            var cacheFileName = "msal_cache.dat";

            //  Ensure the directory exists.
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            //    Build the storage properties without the missing helper methods.
            //    On Windows, the library will automatically use DPAPI for encryption by default.
            var storageProperties =
                new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
                .Build();

            //  Create the MSAL application.
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithRedirectUri("http://localhost")
                .Build();

            //  Wire up the file cache. This part remains the same.
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);

            return new MCAuthenticationService(app);
        }

        public async Task<MCUserInfoModel> Authenticate(bool silent=false)
        {
            AuthenticationResult authResult;

            var accounts = await _msalClient.GetAccountsAsync();

            try
            {
                // Always try silent first
                authResult = await _msalClient.AcquireTokenSilent(new[] { "XboxLive.signin" }, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // If silent fails AND we are in interactive mode, show the browser
                if (!silent)
                {
                    authResult = await _msalClient.AcquireTokenInteractive(new[] { "XboxLive.signin" })
                        .WithUseEmbeddedWebView(false)
                        .ExecuteAsync(CancellationToken.None);
                }
                else
                {
                    // If silent fails and we are in silent-only mode, re-throw the exception
                    throw;
                }
            }
            var xblToken = await GetXboxLiveToken(authResult.AccessToken);
            var (xstsToken, userHash) = await GetXstsToken(xblToken);
            var mcAccessToken = await GetMinecraftToken(userHash, xstsToken);
            var profile = await GetMinecraftProfile(mcAccessToken);

            return new MCUserInfoModel
            {
                Username = profile.GetProperty("name").GetString(),
                Uuid = profile.GetProperty("id").GetString(),
                AccessToken = mcAccessToken
            };
        }

        private async Task<string> GetXboxLiveToken(string msalToken)
        {
            var requestBody = new
            {
                Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = $"d={msalToken}" },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            };
            return await SendJsonRequest<string>(
                "https://user.auth.xboxlive.com/user/authenticate",
                requestBody,
                json => json.RootElement.GetProperty("Token").GetString(),
                "GetXboxLiveToken"
            );
        }

        private async Task<(string, string)> GetXstsToken(string xblToken)
        {
            var requestBody = new
            {
                Properties = new { SandboxId = "RETAIL", UserTokens = new[] { xblToken } },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            };
            return await SendJsonRequest(
                "https://xsts.auth.xboxlive.com/xsts/authorize",
                requestBody,
                json => (
                    json.RootElement.GetProperty("Token").GetString(),
                    json.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()
                ),
                "GetXstsToken"
            );
        }

        private async Task<string> GetMinecraftToken(string userHash, string xstsToken)
        {
            var requestBody = new { identityToken = $"XBL3.0 x={userHash};{xstsToken}" };
            return await SendJsonRequest<string>(
                "https://api.minecraftservices.com/authentication/login_with_xbox",
                requestBody,
                json => json.RootElement.GetProperty("access_token").GetString(),
                "GetMinecraftToken"
            );
        }

        private async Task<JsonElement> GetMinecraftProfile(string mcToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error during GetMinecraftProfile (HTTP {(int)response.StatusCode}). Server says: {errorContent}");
            }

            return (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        }

        // Generic helper method to reduce code duplication and provide detailed error messages
        private async Task<T> SendJsonRequest<T>(string url, object requestBody, Func<JsonDocument, T> extractResult, string methodName)
        {
            var requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                // This is the important part! We read the error body.
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error during {methodName} (HTTP {(int)response.StatusCode}). Server says: {errorContent}");
            }

            var responseJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return extractResult(responseJson);
        }
    }
}
