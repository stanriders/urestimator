using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using UREstimator.Shared;

namespace UREstimator.Server.OsuApi
{
    public class OsuApiProvider
    {
        private readonly ILogger<OsuApiProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        private AccessToken? _accessToken;

        private readonly string? _clientId;
        private readonly string? _clientSecret;

        public OsuApiProvider(ILogger<OsuApiProvider> logger,
            IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;

            _clientId = configuration["ApiV2Client"];
            _clientSecret = configuration["ApiV2Secret"];
        }

        public async Task<Score[]?> GetPlayerScores(string username, string mode)
        {
            await RefreshToken();

            var client = _httpClientFactory.CreateClient("OsuApi");
            client.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(),
                $"Bearer {_accessToken?.Token}");

            var playerData = await _memoryCache.GetOrCreateAsync($"players_{username}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

                var response = await client.GetFromJsonAsync<Player>($"/api/v2/users/{username}");

                return response;
            });

            if (playerData is not null)
            {
                return await _memoryCache.GetOrCreateAsync($"player_scores_{mode}_{playerData.Id}", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

                    try
                    {
                        var playerScores = await client.GetFromJsonAsync<Score[]>(
                                    $"/api/v2/users/{playerData.Id}/scores/best?&limit=100&mode={mode}");

                        return playerScores;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, e.ToString());
                    }

                    return null;
                });
            }

            return null;
        }

        public async Task<Score?> GetScore(long scoreId, string mode)
        {
            await RefreshToken();

            return await _memoryCache.GetOrCreateAsync($"scores_{scoreId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1); // shouldn't really expire at all, but gonna do it anyway to keep the memory in check

                var client = _httpClientFactory.CreateClient("OsuApi");
                client.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {_accessToken?.Token}");
                try
                {
                    var score = await client.GetFromJsonAsync<Score>($"/api/v2/scores/{mode}/{scoreId}");

                    return score;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.ToString());
                }

                return null;
            });
        }

        private async Task RefreshToken()
        {
            if (_accessToken is not null && !_accessToken.Expired)
                return;

            var client = _httpClientFactory.CreateClient("OsuApi");

            var authRequest = new
            {
                client_id = _clientId,
                client_secret = _clientSecret,
                grant_type = "client_credentials",
                scope = "public"
            };

            var request = await client.PostAsJsonAsync("oauth/token", authRequest);
            if (request.IsSuccessStatusCode)
            {
                _accessToken = JsonSerializer.Deserialize<AccessToken>(await request.Content.ReadAsStringAsync());
            }
        }
    }

    public class AccessToken
    {
        private DateTime _expireDate;
        [JsonPropertyName("expires_in")]
        public long ExpiresIn
        {
            get => _expireDate.Ticks;
            set => _expireDate = DateTime.Now.AddSeconds(value);
        }

        public bool Expired => _expireDate < DateTime.Now;

        [JsonPropertyName("access_token")]
        public string Token { get; set; } = null!;
    }
}
