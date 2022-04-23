using System;
using System.Diagnostics;
using Example.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Example.WebApp.Controllers;

[Route("[controller]/[action]")]
public class HomeController : Controller
{
    private readonly IStringLocalizer<HomeController> _localizer;

    public HomeController(IStringLocalizer<HomeController> localizer)
    {
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult About()
    {
        ViewData["Message"] = _localizer["about.description"];

        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        ViewData["Message"] = _localizer["about.renderedOn", new { date = DateTime.Now }];

        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }
}
