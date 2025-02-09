using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using RssNewsApp.Models;
using RssNewsApp.Services;
using Microsoft.Extensions.Http;

namespace WebApplication.Controllers
{
    public class FixturesController : Controller
    {
        private readonly IFixtureService _fixtureService;

        public FixturesController(IFixtureService fixtureService)
        {
            _fixtureService = fixtureService;
        }

        public async Task<IActionResult> Index()
        {
            var fixtures = await _fixtureService.GetFixturesAsync();
            return View(fixtures);
        }

        // Yeni API endpoint ekleyelim
        [HttpGet]
        public async Task<IActionResult> GetLiveScores()
        {
            try
            {
                var fixtures = await _fixtureService.GetFixturesAsync();
                return Json(fixtures);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
} 