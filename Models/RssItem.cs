namespace RssNewsApp.Models
{
    public class RssItem
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        
        // Transfer haberleri için özel alanlar
        public string PlayerName { get; set; } = string.Empty;        // Oyuncu adı
        public string MeetingStatus { get; set; } = string.Empty;     // Görüşme durumu
        public int PostCount { get; set; }            // Gönderi sayısı
        public int Probability { get; set; }          // Olasılık yüzdesi
        public decimal MarketValue { get; set; }      // Piyasa değeri
    }
} 