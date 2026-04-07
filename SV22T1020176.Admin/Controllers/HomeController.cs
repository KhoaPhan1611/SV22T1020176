using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020176.Admin;
using SV22T1020176.Admin.AppCodes;
using SV22T1020176.BusinessLayers;
using SV22T1020176.Models;
using SV22T1020176.Models.Constants;

namespace SV22T1020176.Admin.Controllers;

[Authorize]
public class HomeController : Controller
{
    /// <summary>
    /// Giao diện trang chủ Dashboard (shipper và role không có dashboard:view không được vào)
    /// </summary>
    [AuthorizePermission(Permissions.DashboardView)]
    public async Task<IActionResult> Index()
    {
        var todayRevenue = await DashboardDataService.GetTodayRevenueAsync();
        var todayOrderCount = await DashboardDataService.GetTodayOrderCountAsync();
        var orderCount = await DashboardDataService.GetOrderCountAsync();
        var customerCount = await DashboardDataService.GetCustomerCountAsync();
        var productCount = await DashboardDataService.GetProductCountAsync();
        var topProducts = await DashboardDataService.GetTopSellingProductsAsync(4);
        var pipelineOrders = await DashboardDataService.GetOrdersNeedingProcessingAsync(15);
        var monthlyRevenue = await DashboardDataService.GetMonthlyRevenueAsync(6);

        ViewBag.TodayRevenue = todayRevenue;
        ViewBag.TodayOrderCount = todayOrderCount;
        ViewBag.OrderCount = orderCount;
        ViewBag.CustomerCount = customerCount;
        ViewBag.ProductCount = productCount;
        ViewBag.TopProducts = topProducts;
        ViewBag.PipelineOrders = pipelineOrders;
        ViewBag.MonthlyRevenue = monthlyRevenue;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
