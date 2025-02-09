using RssNewsApp.Models;

namespace RssNewsApp.Services
{
    public interface ISpotifyService
    {
        Task<List<SpotifyTrack>> GetGalatasaraySongsAsync();
    }
} 