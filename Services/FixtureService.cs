using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;  // veya Models.Fixture sınıfının bulunduğu namespace

namespace Services
{
    public class FixtureService : IFixtureService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<FixtureService> _logger;

        public FixtureService(HttpClient httpClient, IConfiguration config, ILogger<FixtureService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<List<MatchFixture>> GetFixturesAsync()
        {
            try
            {
                // Doğrudan API'ye istek yapalım
                var apiUrl = "https://vd.mackolik.com/livedata?group=0";
                
                // Her istekte yeni header eklemek yerine constructor'da bir kez ekleyelim
                if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                }

                var response = await _httpClient.GetStringAsync(apiUrl);
                
                // Yanıtı kontrol edelim
                _logger.LogInformation($"API Yanıtı: {response.Substring(0, Math.Min(500, response.Length))}");
                
                var data = JsonDocument.Parse(response);
                var fixtures = new List<MatchFixture>();

                if (!data.RootElement.TryGetProperty("m", out var matchesElement))
                {
                    _logger.LogWarning("API yanıtında 'm' özelliği bulunamadı. Tam yanıt: {Response}", response);
                    return fixtures;
                }

                foreach (var match in matchesElement.EnumerateArray())
                {
                    try
                    {
                        var matchArray = match.EnumerateArray().ToList();
                        if (matchArray.Count < 37) continue;

                        string league = string.Empty;
                        string competition = string.Empty;
                        
                        if (matchArray.Count > 36 && matchArray[36].ValueKind == JsonValueKind.Array)
                        {
                            var tournamentArray = matchArray[36].EnumerateArray().ToList();
                            if (tournamentArray.Count > 9)
                            {
                                league = tournamentArray[9].ValueKind == JsonValueKind.String
                                    ? tournamentArray[9].GetString() ?? string.Empty
                                    : tournamentArray[9].ToString();
                            }
                        }

                        // Sadece istenen ligleri ve Türk takımlarını filtrele
                        var homeTeam = matchArray[2].GetString() ?? string.Empty;
                        var awayTeam = matchArray[4].GetString() ?? string.Empty;

                        if (!IsDesiredLeague(league) && 
                            !(IsTurkishTeam(homeTeam) || IsTurkishTeam(awayTeam)))
                        {
                            continue;
                        }

                        var fixture = new MatchFixture
                        {
                            Id = matchArray[0].ValueKind == JsonValueKind.Number
                                 ? matchArray[0].GetInt32()
                                 : 0,
                            HomeTeam = matchArray[2].GetString() ?? string.Empty,
                            AwayTeam = matchArray[4].GetString() ?? string.Empty,
                            // Read Date from index 35 (sample shows "09/02/2025")
                            Date = matchArray[35].GetString() ?? string.Empty,
                            // Read Time from index 16 (sample shows "14:30")
                            Time = matchArray[16].GetString() ?? string.Empty,
                            Score = $"{(matchArray[12].ValueKind == JsonValueKind.Number ? matchArray[12].GetInt32().ToString() : matchArray[12].GetString() ?? "0")}-{(matchArray[13].ValueKind == JsonValueKind.Number ? matchArray[13].GetInt32().ToString() : matchArray[13].GetString() ?? "0")}",
                            League = league,
                            Competition = competition
                        };

                        fixtures.Add(fixture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Maç verisi işlenirken hata: {ex.Message}");
                        continue;
                    }
                }

                if (fixtures.Count == 0)
                {
                    _logger.LogWarning("Hiç maç verisi bulunamadı. API yanıtı kontrol edilmeli.");
                }
                else
                {
                    _logger.LogInformation($"Toplam {fixtures.Count} maç bulundu.");
                }

                return fixtures.OrderBy(f => f.Date).ThenBy(f => f.Time).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"API hatası: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private bool IsDesiredLeague(string leagueName)
        {
            var desiredLeagues = new[]
            {
                "Süper Lig",  // Türkiye Süper Lig
                "UEFA Champions League", // Şampiyonlar Ligi
                "UEFA Europa League",    // Avrupa Ligi
                "UEFA Europa Conference League" // Konferans Ligi
            };

            // Önce istenen ligleri kontrol et
            return desiredLeagues.Any(l => leagueName.Equals(l, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTurkishTeam(string teamName)
        {
            var turkishTeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Süper Lig Takımları
                "Galatasaray",
                "Fenerbahçe", 
                "Samsunspor",
                "Eyüpspor",
                "Beşiktaş",
                "Göztepe",
                "Başakşehir",
                "Kasımpaşa",
                "Alanyaspor",
                "Rizespor",
                "Trabzonspor",
                "Gaziantep",
                "Antalyaspor",
                "Konyaspor",
                "Sivasspor",
                "Kayserispor",
                "Bodrum FK",
                "Hatayspor",
                "Adana Demirspor"
            };

            return turkishTeams.Contains(teamName);
        }

        private bool IsImportantTournament(int tournamentId)
        {
            // Turnuva ID'leri:
            // 1 = Süper Lig
            // 2 = Türkiye Kupası
            // 3 = Şampiyonlar Ligi
            // 4 = Avrupa Ligi
            // 5 = Konferans Ligi
            return new[] { 1, 2, 3, 4, 5 }.Contains(tournamentId);
        }

        private string GetTournamentName(int tournamentId)
        {
            return tournamentId switch
            {
                1 => "Süper Lig",
                2 => "Türkiye Kupası", 
                3 => "Şampiyonlar Ligi",
                4 => "Avrupa Ligi",
                5 => "Konferans Ligi",
                _ => "Diğer"
            };
        }

        private List<MatchFixture> ParseJsonResponse(string jsonResponse)
        {
            try 
            {
                var fixtures = new List<MatchFixture>();
                var data = JsonDocument.Parse(jsonResponse);
                
                // API yanıtının yapısını kontrol edelim
                if (!data.RootElement.TryGetProperty("m", out var matchesElement))
                {
                    _logger.LogWarning("API yanıtında 'm' özelliği bulunamadı");
                    return fixtures;
                }

                // Eğer matchesElement bir dizi değilse
                if (matchesElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("API yanıtındaki 'm' özelliği bir dizi değil");
                    return fixtures;
                }

                foreach (var match in matchesElement.EnumerateArray())
                {
                    try
                    {
                        // Maç verilerini güvenli bir şekilde dönüştür
                        var fixture = new MatchFixture
                        {
                            // ... diğer özellikler
                        };
                        fixtures.Add(fixture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Maç işlenirken hata: {ex.Message}");
                        continue;
                    }
                }
                
                return fixtures;
            }
            catch (Exception ex)
            {
                _logger.LogError($"JSON yanıtı işlenirken hata: {ex.Message}");
                throw;
            }
        }

        private Fixture ParseFixtureData(JsonElement data)
        {
            try 
            {
                var array = data.EnumerateArray().ToList();
                
                return new Fixture
                {
                    Score1 = array[12].ValueKind == JsonValueKind.Number
                             ? array[12].GetInt32().ToString()
                             : array[12].GetString() ?? "0",
                             
                    Score2 = array[13].ValueKind == JsonValueKind.Number
                             ? array[13].GetInt32().ToString()
                             : array[13].GetString() ?? "0",
                             
                    HalfScore1 = array[33].ValueKind == JsonValueKind.Number
                                 ? array[33].GetInt32().ToString()
                                 : array[33].GetString() ?? "0",
                                 
                    HalfScore2 = array[34].ValueKind == JsonValueKind.Number
                                 ? array[34].GetInt32().ToString()
                                 : array[34].GetString() ?? "0",
                                 
                    Odds = new List<string>
                    {
                        array[27].ValueKind == JsonValueKind.Number ? 
                            array[27].GetDouble().ToString() : array[27].GetString() ?? "0.0",
                        array[28].ValueKind == JsonValueKind.Number ? 
                            array[28].GetDouble().ToString() : array[28].GetString() ?? "0.0",
                        array[29].ValueKind == JsonValueKind.Number ? 
                            array[29].GetDouble().ToString() : array[29].GetString() ?? "0.0",
                        array[30].ValueKind == JsonValueKind.Number ? 
                            array[30].GetDouble().ToString() : array[30].GetString() ?? "0.0",
                        array[31].ValueKind == JsonValueKind.Number ? 
                            array[31].GetDouble().ToString() : array[31].GetString() ?? "0.0"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Maç verisi işlenirken hata: {ex.Message}, Veri: {data}");
                throw;
            }
        }
    }
}
