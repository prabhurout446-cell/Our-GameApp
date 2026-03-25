using Newtonsoft.Json;
using Our11.Models;

namespace Our11.Services
{
    public interface ICricketApiService
    {
        Task<List<Match>> FetchUpcomingMatchesAsync();
        Task<List<Player>> FetchPlayersForMatchAsync(string externalMatchId);
        Task<string?> FetchLiveScoreAsync(string externalMatchId);
    }

    public class CricketApiService : ICricketApiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<CricketApiService> _logger;
        private const string BASE = "https://api.cricapi.com/v1";

        public CricketApiService(HttpClient http, IConfiguration config, ILogger<CricketApiService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        private string ApiKey => _config["CricApi:ApiKey"] ?? "";

        public async Task<List<Match>> FetchUpcomingMatchesAsync()
        {
            try
            {
                var url = $"{BASE}/matches?apikey={ApiKey}&offset=0";
                var json = await _http.GetStringAsync(url);
                var resp = JsonConvert.DeserializeObject<CricApiResponse<List<CricApiMatch>>>(json);
                if (resp?.Status != "success" || resp.Data == null) return GetFallbackMatches();

                return resp.Data
                    .Where(m => m.MatchStarted == false || m.MatchEnded == false)
                    .Take(20)
                    .Select(m => new Match
                    {
                        ExternalMatchId = m.Id,
                        Team1 = m.Teams?.Count > 0 ? m.Teams[0] : "TBD",
                        Team2 = m.Teams?.Count > 1 ? m.Teams[1] : "TBD",
                        Sport = "Cricket",
                        MatchType = m.MatchType ?? "T20",
                        Venue = m.Venue ?? "",
                        StartTime = m.DateTimeGMT != null ? DateTime.Parse(m.DateTimeGMT) : DateTime.UtcNow.AddHours(2),
                        Status = m.MatchStarted && !m.MatchEnded ? "Live" : m.MatchEnded ? "Completed" : "Upcoming",
                        Score = m.Score?.Count > 0 ? string.Join(" | ", m.Score.Select(s => $"{s.Inning}: {s.R}/{s.W} ({s.O} ov)")) : null
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching matches from CricAPI");
                return GetFallbackMatches();
            }
        }

        public async Task<List<Player>> FetchPlayersForMatchAsync(string externalMatchId)
        {
            try
            {
                var url = $"{BASE}/match_info?apikey={ApiKey}&id={externalMatchId}";
                var json = await _http.GetStringAsync(url);
                var resp = JsonConvert.DeserializeObject<CricApiResponse<CricApiMatchInfo>>(json);
                if (resp?.Data == null) return new List<Player>();

                var players = new List<Player>();
                int i = 0;
                foreach (var squad in resp.Data.Squad ?? new List<CricApiSquad>())
                {
                    foreach (var p in squad.Players ?? new List<CricApiPlayer>())
                    {
                        players.Add(new Player
                        {
                            Name = p.Name,
                            Team = squad.TeamName,
                            Role = MapRole(p.Role),
                            Credits = GetCredits(p.Role, i++),
                            SelectionPercentage = Random.Shared.Next(5, 85),
                            IsPlaying = true
                        });
                    }
                }
                return players.Count > 0 ? players : GetFallbackPlayers(externalMatchId);
            }
            catch
            {
                return GetFallbackPlayers(externalMatchId);
            }
        }

        public async Task<string?> FetchLiveScoreAsync(string externalMatchId)
        {
            try
            {
                var url = $"{BASE}/match_info?apikey={ApiKey}&id={externalMatchId}";
                var json = await _http.GetStringAsync(url);
                var resp = JsonConvert.DeserializeObject<CricApiResponse<CricApiMatchInfo>>(json);
                if (resp?.Data?.Score == null) return null;
                return string.Join(" | ", resp.Data.Score.Select(s => $"{s.Inning}: {s.R}/{s.W} ({s.O} ov)"));
            }
            catch { return null; }
        }

        private string MapRole(string? role) => role?.ToLower() switch
        {
            var r when r?.Contains("wicket") == true => "WK",
            var r when r?.Contains("bat") == true => "BAT",
            var r when r?.Contains("all") == true => "ALL",
            var r when r?.Contains("bowl") == true => "BOWL",
            _ => "BAT"
        };

        private decimal GetCredits(string? role, int i) => role?.ToLower() switch
        {
            var r when r?.Contains("all") == true => 9.0m + (i % 2),
            var r when r?.Contains("bat") == true => 8.5m + (i % 3 * 0.5m),
            var r when r?.Contains("bowl") == true => 8.0m + (i % 3 * 0.5m),
            _ => 8.0m
        };

        // ─── Fallback data when API key is not set ─────────────────────────────
        private List<Match> GetFallbackMatches() => new()
        {
            new Match { ExternalMatchId = "demo-1", Team1 = "India", Team2 = "Australia", Sport = "Cricket", MatchType = "T20", Venue = "Wankhede Stadium, Mumbai", StartTime = DateTime.UtcNow.AddHours(3), Status = "Upcoming" },
            new Match { ExternalMatchId = "demo-2", Team1 = "England", Team2 = "Pakistan", Sport = "Cricket", MatchType = "ODI", Venue = "Lord's, London", StartTime = DateTime.UtcNow.AddHours(6), Status = "Upcoming" },
            new Match { ExternalMatchId = "demo-3", Team1 = "South Africa", Team2 = "New Zealand", Sport = "Cricket", MatchType = "T20", Venue = "Newlands, Cape Town", StartTime = DateTime.UtcNow.AddHours(12), Status = "Upcoming" },
            new Match { ExternalMatchId = "demo-4", Team1 = "West Indies", Team2 = "Sri Lanka", Sport = "Cricket", MatchType = "T20", Venue = "Kensington Oval", StartTime = DateTime.UtcNow.AddHours(-1), Status = "Live", Score = "WI: 145/6 (18.2 ov) | SL: yet to bat" },
            new Match { ExternalMatchId = "demo-5", Team1 = "Bangladesh", Team2 = "Afghanistan", Sport = "Cricket", MatchType = "ODI", Venue = "Shere Bangla Stadium", StartTime = DateTime.UtcNow.AddHours(24), Status = "Upcoming" },
        };

        private List<Player> GetFallbackPlayers(string matchId)
        {
            var teams = matchId == "demo-1" ? new[] { "India", "Australia" }
                      : matchId == "demo-2" ? new[] { "England", "Pakistan" }
                      : matchId == "demo-3" ? new[] { "South Africa", "New Zealand" }
                      : matchId == "demo-4" ? new[] { "West Indies", "Sri Lanka" }
                      : new[] { "Team A", "Team B" };

            var players = new List<Player>();
            var roles = new[] { "WK", "BAT", "BAT", "BAT", "ALL", "ALL", "BOWL", "BOWL", "BOWL", "BOWL", "BAT" };
            var names1 = new[] { "R Sharma", "V Kohli", "S Gill", "K Rahul", "H Pandya", "R Jadeja", "J Bumrah", "M Shami", "Y Chahal", "K Yadav", "D Karthik" };
            var names2 = new[] { "D Warner", "S Smith", "M Labuschagne", "T Head", "C Green", "M Stoinis", "P Cummins", "J Hazlewood", "M Starc", "A Zampa", "M Inglis" };

            for (int i = 0; i < 11; i++)
            {
                players.Add(new Player { Name = names1[i], Team = teams[0], Role = roles[i], Credits = 7.5m + (i % 4) * 0.5m, SelectionPercentage = Random.Shared.Next(10, 90), IsPlaying = true });
                players.Add(new Player { Name = names2[i], Team = teams[1], Role = roles[i], Credits = 7.5m + (i % 4) * 0.5m, SelectionPercentage = Random.Shared.Next(10, 90), IsPlaying = true });
            }
            return players;
        }
    }

    // ─── API DTOs ──────────────────────────────────────────────────────────────
    class CricApiResponse<T>
    {
        [JsonProperty("status")] public string? Status { get; set; }
        [JsonProperty("data")] public T? Data { get; set; }
    }
    class CricApiMatch
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("matchType")] public string? MatchType { get; set; }
        [JsonProperty("status")] public string? Status { get; set; }
        [JsonProperty("venue")] public string? Venue { get; set; }
        [JsonProperty("date")] public string? Date { get; set; }
        [JsonProperty("dateTimeGMT")] public string? DateTimeGMT { get; set; }
        [JsonProperty("teams")] public List<string>? Teams { get; set; }
        [JsonProperty("score")] public List<CricApiScore>? Score { get; set; }
        [JsonProperty("matchStarted")] public bool MatchStarted { get; set; }
        [JsonProperty("matchEnded")] public bool MatchEnded { get; set; }
    }
    class CricApiScore
    {
        [JsonProperty("r")] public int R { get; set; }
        [JsonProperty("w")] public int W { get; set; }
        [JsonProperty("o")] public double O { get; set; }
        [JsonProperty("inning")] public string? Inning { get; set; }
    }
    class CricApiMatchInfo
    {
        [JsonProperty("squad")] public List<CricApiSquad>? Squad { get; set; }
        [JsonProperty("score")] public List<CricApiScore>? Score { get; set; }
    }
    class CricApiSquad
    {
        [JsonProperty("team")] public string TeamName { get; set; } = "";
        [JsonProperty("players")] public List<CricApiPlayer>? Players { get; set; }
    }
    class CricApiPlayer
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("role")] public string? Role { get; set; }
    }
}
