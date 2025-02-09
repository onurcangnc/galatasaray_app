using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using RssNewsApp.Models;

namespace RssNewsApp.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _accessToken = string.Empty;
        private DateTime _tokenExpiration;

        public SpotifyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private async Task EnsureAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow)
                return;

            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Spotify client credentials are not configured.");
            }

            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                })
            };

            tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>();
            
            if (tokenJson == null)
                throw new InvalidOperationException("Failed to get Spotify access token.");

            _accessToken = tokenJson.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(3600);
        }

        public async Task<List<SpotifyTrack>> GetGalatasaraySongsAsync()
        {
            await EnsureAccessTokenAsync();

            // Galatasaray playlist ID'si
            var playlistId = "2D7Uqngi8BrHAUzNitR9tn";
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.GetAsync(
                $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?fields=items(track(name,artists(name)))"
            );

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

            if (json == null) return new List<SpotifyTrack>();

            var tracks = new List<SpotifyTrack>();
            var items = json.RootElement.GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                var track = item.GetProperty("track");
                var title = track.GetProperty("name").GetString() ?? "";
                var artist = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? "";

                tracks.Add(new SpotifyTrack 
                { 
                    Title = title,
                    Artist = artist
                });
            }

            return tracks;
        }
    }
} 