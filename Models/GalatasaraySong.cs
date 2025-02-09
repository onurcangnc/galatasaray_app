public class GalatasaraySong
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string AlbumImage { get; set; } = string.Empty;
    public string SpotifyUrl { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }  // Nullable çünkü bazı şarkılarda preview olmayabilir
} 