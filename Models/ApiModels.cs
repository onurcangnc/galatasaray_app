using System.Text.Json;

public class ApiResponse
{
    public required List<FixtureResponse> Response { get; set; }
    public JsonElement m { get; set; }
}

public class FixtureResponse
{
    public required FixtureDetails Fixture { get; set; }
    public required LeagueDetails League { get; set; }
    public required Teams Teams { get; set; }
}

public class FixtureDetails
{
    public required string Date { get; set; }
    public required Venue Venue { get; set; }
}

public class LeagueDetails
{
    public required string Name { get; set; }
}

public class Teams
{
    public required Team Home { get; set; }
    public required Team Away { get; set; }
}

public class Team
{
    public required string Name { get; set; }
}

public class Venue
{
    public required string Name { get; set; }
}

namespace Services
{
    public class MackolikFixtureService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://vd.mackolik.com/livedata?group=0";
        
        public MackolikFixtureService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<GalatasarayFixture>> GetGalatasarayFixtures()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(API_URL);
                var data = JsonDocument.Parse(response);
                
                var fixtures = new List<GalatasarayFixture>();
                
                var matches = data.RootElement.GetProperty("m").EnumerateArray();
                
                foreach (var match in matches)
                {
                    var homeTeam = match.GetProperty("homeTeam").GetString() ?? "Bilinmiyor";
                    var awayTeam = match.GetProperty("awayTeam").GetString() ?? "Bilinmiyor";
                    
                    if (homeTeam == "Galatasaray" || awayTeam == "Galatasaray")
                    {
                        var date = match.GetProperty("date").GetString() ?? DateTime.Now.ToString("yyyy-MM-dd");
                        var time = match.GetProperty("time").GetString() ?? "00:00";
                        var competition = match.GetProperty("competition").GetString() ?? "Bilinmiyor";
                        var status = match.GetProperty("status").GetString() ?? "Belirlenmedi";

                        fixtures.Add(new GalatasarayFixture
                        {
                            HomeTeam = homeTeam,
                            AwayTeam = awayTeam,
                            Date = date,
                            Time = time,
                            Competition = competition,
                            Status = status
                        });
                    }
                }
                
                return fixtures;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Maç verileri alınırken hata oluştu: {ex.Message}");
                return new List<GalatasarayFixture>();
            }
        }
    }
}

public class GalatasarayFixture
{
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public required string Date { get; set; }
    public required string Time { get; set; }
    public required string Competition { get; set; }
    public required string Status { get; set; }
} 