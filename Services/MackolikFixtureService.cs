using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using RssNewsApp.Models;
using RssNewsApp.Services;

namespace RssNewsApp.Services
{
    public class MackolikFixtureService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://vd.mackolik.com/livedata?group=0";
        
        public MackolikFixtureService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<MatchFixture>> GetSuperLigFixtures()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(API_URL);
                var data = JsonDocument.Parse(response);
                var fixtures = new List<MatchFixture>();

                // "m" array'ini al
                var matches = data.RootElement.GetProperty("m").EnumerateArray();

                foreach (var match in matches)
                {
                    // Süper Lig maçı kontrolü
                    var matchInfo = match.EnumerateArray().ToList();
                    if (matchInfo.Count < 3) continue;

                    // Türkiye Süper Lig kontrolü [1,"Türkiye",1]
                    var leagueInfo = matchInfo[^2].EnumerateArray().Take(3).ToList();
                    if (leagueInfo.Count != 3) continue;
                    
                    if (leagueInfo[0].GetInt32() == 1 && 
                        leagueInfo[1].GetString() == "Türkiye" && 
                        leagueInfo[2].GetInt32() == 1)
                    {
                        var fixture = new MatchFixture
                        {
                            HomeTeam = matchInfo[2].GetString() ?? "Bilinmiyor",
                            AwayTeam = matchInfo[4].GetString() ?? "Bilinmiyor",
                            Time = matchInfo[10].GetString() ?? "00:00",
                            Date = matchInfo[^1].GetString() ?? DateTime.Now.ToString("dd/MM/yyyy"),
                            League = "Süper Lig"
                        };

                        fixtures.Add(fixture);
                    }
                }

                return fixtures;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Maç verileri alınırken hata oluştu: {ex.Message}");
                return new List<MatchFixture>();
            }
        }

        private string GetMatchStatus(string status)
        {
            return status switch
            {
                "MS" => "Maç Sonu",
                "IY" => "İlk Yarı",
                "D" => "Devre Arası",
                "" => "Başlamadı",
                _ => status
            };
        }
    }
} 