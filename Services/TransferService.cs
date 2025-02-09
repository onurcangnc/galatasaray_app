using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Xml;
using Models;
using RssNewsApp.Services;
using RssNewsApp.Models;

namespace RssNewsApp.Services
{
    public interface ITransferService
    {
        Task<List<TransferNews>> GetTransferNewsAsync();
    }

    public class TransferService : ITransferService
    {
        private readonly IRssService _rssService;
        private readonly ILogger<TransferService> _logger;

        public TransferService(IRssService rssService, ILogger<TransferService> logger)
        {
            _rssService = rssService;
            _logger = logger;
        }

        public async Task<List<TransferNews>> GetTransferNewsAsync()
        {
            var allNews = await _rssService.GetTransferNewsAsync();
            var transferNews = new List<TransferNews>();

            foreach (var news in allNews)
            {
                try
                {
                    var description = news.Description;
                    var lines = description.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("Oyuncu:"))
                        {
                            var parts = line.Split(',').Select(p => p.Trim()).ToList();
                            var playerName = parts.FirstOrDefault(p => p.StartsWith("Oyuncu:"))?.Split(':')[1].Trim() ?? "";
                            var postCount = 0;
                            var probability = "0%";
                            var marketValue = "Bilinmiyor";

                            foreach (var part in parts)
                            {
                                if (part.StartsWith("Gönderi:"))
                                {
                                    _ = int.TryParse(part.Split(':')[1].Trim(), out postCount);
                                }
                                else if (part.StartsWith("Olasılık:"))
                                {
                                    probability = part.Split(':')[1].Trim();
                                }
                                else if (part.StartsWith("Piyasa değeri:"))
                                {
                                    marketValue = part.Split(':')[1].Trim();
                                }
                            }

                            if (!string.IsNullOrEmpty(playerName))
                            {
                                transferNews.Add(new TransferNews
                                {
                                    PlayerName = playerName,
                                    Status = "Takip Ediliyor",
                                    PostCount = postCount,
                                    Probability = int.Parse(probability.TrimEnd('%')),
                                    MarketValue = decimal.Parse(marketValue.Replace(" mil. €", "")),
                                    PublishDate = news.PublishDate
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Haber işlenirken hata oluştu: {news.Description}");
                    continue;
                }
            }

            return transferNews;
        }

        private string ExtractValue(string text, string key)
        {
            var startIndex = text.IndexOf(key);
            if (startIndex == -1) return "Bilinmiyor";
            
            startIndex += key.Length;
            var endIndex = text.IndexOf(',', startIndex);
            if (endIndex == -1) endIndex = text.IndexOf('\n', startIndex);
            if (endIndex == -1) endIndex = text.Length;
            
            return text.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private string ExtractPlayerName(string title)
        {
            // Basit bir örnek: "Transfer: Zaha Galatasaray'a geliyor" -> "Zaha"
            var words = title.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].ToLower().Contains("transfer"))
                {
                    if (i + 1 < words.Length)
                        return words[i + 1];
                }
            }
            return "Bilinmiyor";
        }

        private string DetermineStatus(string title)
        {
            var lowerTitle = title.ToLower();
            if (lowerTitle.Contains("görüşme")) return "Görüşmede";
            if (lowerTitle.Contains("anlaşma")) return "Anlaşma Aşamasında";
            if (lowerTitle.Contains("imza")) return "İmza Aşamasında";
            return "Takip Ediliyor";
        }

        private string DetermineTransferProbability(string title)
        {
            var lowerTitle = title.ToLower();
            if (lowerTitle.Contains("imza") || lowerTitle.Contains("anlaşma")) return "90%";
            if (lowerTitle.Contains("görüşme")) return "60%";
            if (lowerTitle.Contains("ilgileniyor")) return "30%";
            return "10%";
        }
    }
} 