using System;
using System.Xml;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using RssNewsApp.Models;
using System.Net.Http;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RssNewsApp.Services
{
    public class RssService : IRssService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<RssService> _logger;
        private List<RssItem> _newsCache = new();
        private List<RssItem> _transfersCache = new();

        public RssService(HttpClient httpClient, IConfiguration config, ILogger<RssService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<List<RssItem>> GetNewsAsync()
        {
            if (!_newsCache.Any())
            {
                await UpdateNewsAsync();
            }
            return _newsCache;
        }

        public async Task<List<RssItem>> GetTransferNewsAsync()
        {
            try 
            {
                string feedUrl = _config.GetValue<string>("RSSFeeds:Transfers") ?? "";
                using var client = new HttpClient();
                var response = await client.GetStringAsync(feedUrl);
                
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                
                var items = xmlDoc.SelectNodes("//item");
                var rssItems = new List<RssItem>();
                
                if (items != null)
                {
                    foreach (XmlNode item in items)
                    {
                        var description = item.SelectSingleNode("description")?.InnerText ?? "";
                        
                        // Transfer bilgilerini description'dan parse et
                        var playerInfo = ParseTransferInfo(description);
                        
                        rssItems.Add(new RssItem
                        {
                            Title = item.SelectSingleNode("title")?.InnerText ?? string.Empty,
                            Link = item.SelectSingleNode("link")?.InnerText ?? string.Empty,
                            Description = description,
                            PublishDate = DateTime.Parse(item.SelectSingleNode("pubDate")?.InnerText ?? DateTime.Now.ToString()),
                            PlayerName = playerInfo.PlayerName,
                            MeetingStatus = playerInfo.MeetingStatus,
                            PostCount = playerInfo.PostCount,
                            Probability = playerInfo.Probability,
                            MarketValue = playerInfo.MarketValue
                        });
                    }
                }
                
                return rssItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer RSS feed alınırken hata oluştu");
                throw;
            }
        }

        public async Task UpdateNewsAsync()
        {
            try
            {
                string feedUrl = _config.GetValue<string>("RSSFeeds:News") ?? "";
                using var stream = await _httpClient.GetStreamAsync(feedUrl);
                using var reader = XmlReader.Create(stream);
                var feed = SyndicationFeed.Load(reader);
                
                _newsCache.Clear();
                foreach (var item in feed.Items)
                {
                    _newsCache.Add(new RssItem
                    {
                        Title = item.Title.Text,
                        Description = item.Summary.Text,
                        Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
                        PublishDate = item.PublishDate.DateTime
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RSS feed güncellenirken hata oluştu");
                throw;
            }
        }

        public async Task UpdateTransferNewsAsync()
        {
            try
            {
                string feedUrl = _config.GetValue<string>("RSSFeeds:Transfers") ?? "";
                using var client = new HttpClient();
                var response = await client.GetStringAsync(feedUrl);
                
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                
                var items = xmlDoc.SelectNodes("//item");
                _transfersCache.Clear();
                
                if (items != null)
                {
                    foreach (XmlNode item in items)
                    {
                        var description = item.SelectSingleNode("description")?.InnerText ?? "";
                        var playerInfo = ParseTransferInfo(description);
                        
                        _transfersCache.Add(new RssItem
                        {
                            Title = item.SelectSingleNode("title")?.InnerText ?? string.Empty,
                            Link = item.SelectSingleNode("link")?.InnerText ?? string.Empty,
                            Description = description,
                            PublishDate = DateTime.Parse(item.SelectSingleNode("pubDate")?.InnerText ?? DateTime.Now.ToString()),
                            PlayerName = playerInfo.PlayerName,
                            MeetingStatus = playerInfo.MeetingStatus,
                            PostCount = playerInfo.PostCount,
                            Probability = playerInfo.Probability,
                            MarketValue = playerInfo.MarketValue
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer RSS feed güncellenirken hata oluştu");
                throw;
            }
        }

        private (string PlayerName, string MeetingStatus, int PostCount, int Probability, decimal MarketValue) ParseTransferInfo(string description)
        {
            try
            {
                // Description'dan bilgileri ayıkla
                var playerMatch = Regex.Match(description, @"Oyuncu:\s*(.*?),");
                var meetingMatch = Regex.Match(description, @"görüşmede:\s*(.*?),");
                var postMatch = Regex.Match(description, @"Gönderi:\s*(\d+)");
                var probMatch = Regex.Match(description, @"Olasılık:\s*(\d+)%");
                var valueMatch = Regex.Match(description, @"Piyasa değeri:\s*([\d.]+)\s*mil\.\s*€");

                return (
                    PlayerName: playerMatch.Success ? playerMatch.Groups[1].Value.Trim() : "",
                    MeetingStatus: meetingMatch.Success ? meetingMatch.Groups[1].Value.Trim() : "",
                    PostCount: postMatch.Success ? int.Parse(postMatch.Groups[1].Value) : 0,
                    Probability: probMatch.Success ? int.Parse(probMatch.Groups[1].Value) : 0,
                    MarketValue: valueMatch.Success ? decimal.Parse(valueMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0
                );
            }
            catch
            {
                return ("", "", 0, 0, 0);
            }
        }
    }
}
