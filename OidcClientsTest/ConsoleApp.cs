using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OidcClientsTest
{
    public class ConsoleApp
    {
        private readonly ILogger<ConsoleApp> _logger;
        private readonly ConsoleConfig _config;

        public ConsoleApp(IOptions<ConsoleConfig> configuration, ILogger<ConsoleApp> logger)
        {
            _logger = logger;
            _config = configuration.Value;
        }
        
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RunAsync");
            _logger.LogInformation($"IOptions: {_config}");


            using var client = new HttpClient();
            _logger.LogInformation($"Discovering authority:{_config.Authority}");
            var disco = await client.GetDiscoveryDocumentAsync(_config.Authority, cancellationToken);
            if (disco.IsError)
            {
                _logger.LogError(disco.Error);
            }
            else
            {
                foreach (ClientConfig clientConfig in _config.Clients)
                {
                    foreach (var grantType in clientConfig.GrantTypes)
                    {
                        _logger.LogInformation($"ClientId:{clientConfig.Id} grant:{grantType} endpoint:{disco.TokenEndpoint} scopes:{string.Join(" ", clientConfig.AllowedScopes)}");
                        TokenResponse? tokenResponse;
                        switch (grantType)
                        {
                            case "authorization_code":
                                _logger.LogInformation($"using AuthorizeEndpoint:{disco.AuthorizeEndpoint}");
                                tokenResponse = await TestAuthCode(clientConfig, client, disco.TokenEndpoint, disco.AuthorizeEndpoint);

                                if (!tokenResponse.IsError)
                                {
                                    foreach (var apiTest in clientConfig.ApiTests)
                                    {
                                        await TestAccessTokenToApi(tokenResponse.AccessToken, apiTest);
                                    }
                                }
                                else
                                {
                                    _logger.LogError($"{clientConfig.Id} {tokenResponse.ErrorDescription}");
                                }
                                break;
                            case "client_credentials":
                                tokenResponse = await client.RequestClientCredentialsTokenAsync(
                                    new ClientCredentialsTokenRequest
                                    {
                                        Address = disco.TokenEndpoint,
                                        ClientId = clientConfig.Id,
                                        ClientSecret = clientConfig.Password,
                                        Scope = string.Join(" ", clientConfig.AllowedScopes)
                                    }, cancellationToken);
                                if (!tokenResponse.IsError)
                                {
                                    foreach (var apiTest in clientConfig.ApiTests)
                                    {
                                        await TestAccessTokenToApi(tokenResponse.AccessToken, apiTest);
                                    }
                                }
                                else
                                {
                                    _logger.LogError($"{clientConfig.Id} {tokenResponse.ErrorDescription}");
                                }
                                break;
                            default:
                                throw new Exception($"{grantType} not handled");

                        }
                        WriteResponse(tokenResponse);


                        if (tokenResponse.IdentityToken != null)
                        {
                            _logger.LogInformation("UserInfoRequest");
                            var response = await client.GetUserInfoAsync(new UserInfoRequest
                            {
                                ClientId = clientConfig.Id,
                                ClientSecret = clientConfig.Password,
                                Address = disco.UserInfoEndpoint,
                                Token = tokenResponse.AccessToken
                            }, cancellationToken);
                            _logger.LogInformation(response.Raw);

                        }

                        if (tokenResponse.RefreshToken != null)
                        {
                            _logger.LogInformation("RefreshTokenRequest");
                            var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
                            {
                                ClientId = clientConfig.Id,
                                ClientSecret = clientConfig.Password,
                                Address = disco.TokenEndpoint,
                                RefreshToken = tokenResponse.RefreshToken
                            }, cancellationToken);
                            _logger.LogInformation(response.Raw);

                        }
                        _logger.LogInformation($"Client {clientConfig.Id} tests ended");
                    }
                }
            }
        }

        private async Task<TokenResponse> TestAuthCode(ClientConfig clientConfig, HttpClient client, string discoTokenEndpoint, string discoAuthorizeEndpoint)
        {
            var ru = new RequestUrl(discoAuthorizeEndpoint);

            string codeVerifier = CryptoRandom.CreateUniqueId(50);
            string codeChallenge;
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64Url.Encode(challengeBytes);
            }

            var url = ru.CreateAuthorizeUrl(
                codeChallenge: codeChallenge,
                codeChallengeMethod: "S256",
                clientId: clientConfig.Id,
                responseType: "code",
                redirectUri: clientConfig.RedirectUris[0],
                scope: string.Join(" ", clientConfig.AllowedScopes));
            OpenUrl(url);
            _logger.LogInformation(url);
            //dopo il login l'utente viene rediretto alla pagina del redirectUri e tra i parametri in querystring c'è il code
            _logger.LogInformation("insert code:");
            var code = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(code))
            {
                code = "code_missing";
            }
            return await client.RequestAuthorizationCodeTokenAsync(
                new AuthorizationCodeTokenRequest
                {
                    Address = discoTokenEndpoint,
                    ClientId = clientConfig.Id,
                    ClientSecret = clientConfig.Password,
                    RedirectUri = clientConfig.RedirectUris[0],
                    Code = code,
                    CodeVerifier = codeVerifier
                });
        }


        private void WriteResponse(TokenResponse tokenResponse)
        {
            if (tokenResponse.IsError)
            {
                _logger.LogError(tokenResponse.Error);
            }
            else
            {
                _logger.LogInformation(tokenResponse.Json.ToString());
            }
        }
        private async Task TestAccessTokenToApi(string accessToken, string apiUrl)
        {
            _logger.LogInformation($"calling api {apiUrl} with token:{accessToken}");
            using var client = new HttpClient();
            client.SetBearerToken(accessToken);
            var response = await client.GetAsync(apiUrl);


            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"REQUEST FAILED");
            }
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"{apiUrl} {response.StatusCode} {response.ReasonPhrase}");
            _logger.LogInformation(content);
        }

        private static void OpenUrl(string webpageurl)
        {
            string url = webpageurl.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

    }
}