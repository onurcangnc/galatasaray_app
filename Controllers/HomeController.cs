using Microsoft.AspNetCore.Mvc;
using RssNewsApp.Models;
using RssNewsApp.Services;
using System.Linq;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RssNewsApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRssService _rssService;
        private readonly ISpotifyService _spotifyService;
        private const int PAGE_SIZE = 10; // Her sayfada gösterilecek haber sayısı

        public HomeController(IRssService rssService, ISpotifyService spotifyService)
        {
            _rssService = rssService;
            _spotifyService = spotifyService;
        }

        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            var allNews = await _rssService.GetNewsAsync() ?? new List<RssItem>();
            
            // Arama filtrelemesi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allNews = allNews.Where(x => 
                    (x.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) || 
                    (x.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();
            }

            // Sayfalama
            var totalItems = allNews.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)PAGE_SIZE);
            var items = allNews.Skip((page - 1) * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_NewsItems", items);
            }

            return View(items);
        }

        public IActionResult About()
        {
            return View(); // Views/Home/About.cshtml'i kullanacak
        }

        public async Task<IActionResult> Privacy()
        {
            var songs = await _spotifyService.GetGalatasaraySongsAsync();
            return View(songs);
        }
    }
}
