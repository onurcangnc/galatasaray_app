using Microsoft.AspNetCore.Mvc;
using RssNewsApp.Services;
using RssNewsApp.Models;
using System.Threading.Tasks;
using System.Linq;

namespace RssNewsApp.Controllers
{
    public class TransferController : Controller
    {
        private readonly IRssService _rssService;

        public TransferController(IRssService rssService)
        {
            _rssService = rssService;
        }

        [HttpGet("/gstransferhaber")]
        public async Task<IActionResult> Index()
        {
            var rssItems = await _rssService.GetTransferNewsAsync();
            
            // RssItem'ları TransferNews'e dönüştür
            var transferNews = rssItems.Select(item => new TransferNews
            {
                PlayerName = item.PlayerName,
                Status = item.MeetingStatus,
                PostCount = item.PostCount,
                Probability = item.Probability,
                MarketValue = item.MarketValue,
                PublishDate = item.PublishDate
            }).ToList();

            return View(transferNews);
        }
    }
} 