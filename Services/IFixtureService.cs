using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFixtureService
{
    Task<List<MatchFixture>> GetFixturesAsync();
}

public class MackolikFixtureService : IFixtureService
{
    private readonly HttpClient _httpClient;
    private const string API_URL = "https://vd.mackolik.com/livedata?group=0";
    
    public MackolikFixtureService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<MatchFixture>> GetFixturesAsync()
    {
        // Geçici olarak boş liste dönüyoruz
        return await Task.FromResult(new List<MatchFixture>());
    }
} 