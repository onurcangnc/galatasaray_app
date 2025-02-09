namespace RssNewsApp.Models
{
    public class TransferNews
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public int Probability { get; set; }
        public decimal MarketValue { get; set; }
        public DateTime PublishDate { get; set; }
    }
} 