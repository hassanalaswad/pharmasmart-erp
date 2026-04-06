using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class ShortageForecastReportController : BaseController
    {
        public ShortageForecastReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/ShortageForecastReport")]
        [HttpGet("/ShortageForecastReport/Index")]
        [HasPermission("ShortageForecast", "View")]
        public async Task<IActionResult> Index()
        {
            try
            {
                int branchId = ReportScopeId;
                var drugs = await _context.Drugs.Where(d => d.IsActive == true).ToListAsync();
                var forecastList = new List<ShortageForecastViewModel>();

                foreach (var drug in drugs)
                {
                    var historyQuery = _context.Saledetails.Include(sd => sd.Sale)
                        .Where(sd => sd.DrugId == drug.DrugId).AsQueryable();

                    if (branchId != 0) historyQuery = historyQuery.Where(sd => sd.Sale.BranchId == branchId);

                    var rawHistory = await historyQuery.Select(sd => new { sd.Sale.SaleDate, sd.Quantity }).ToListAsync();
                    var salesHistory = rawHistory
                        .Select(sd => new { date = sd.SaleDate.ToString("yyyy-MM-dd"), quantity = sd.Quantity })
                        .ToList();

                    decimal predictedDemand = 0;

                    if (salesHistory != null && salesHistory.Count > 5)
                        predictedDemand = await CallProphetModel(salesHistory);
                    else if (salesHistory != null && salesHistory.Any())
                        predictedDemand = (decimal)salesHistory.Average(x => x.quantity) * 30;

                    var stockQuery = _context.Branchinventory.Where(p => p.DrugId == drug.DrugId).AsQueryable();
                    if (branchId != 0) stockQuery = stockQuery.Where(p => p.BranchId == branchId);

                    var currentStock = await stockQuery.SumAsync(p => p.StockQuantity);

                    forecastList.Add(new ShortageForecastViewModel
                    {
                        DrugId = drug.DrugId,
                        DrugName = drug.DrugName,
                        CurrentStock = currentStock,
                        MonthlyForecast = Math.Round(predictedDemand, 0),
                        SuggestedOrder = (predictedDemand > currentStock) ? (predictedDemand - currentStock) : 0,
                        RiskLevel = (currentStock < (predictedDemand * 0.2m)) ? "High" : (currentStock < predictedDemand) ? "Medium" : "Safe"
                    });
                }
                return View("~/Views/Report/ShortageForecast.cshtml", forecastList.OrderByDescending(x => x.SuggestedOrder).ToList());
            }
            catch (Exception ex)
            {
                return Content($"Internal Server Error: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        private async Task<decimal> CallProphetModel(object historyData)
        {
            try
            {
                string jsonInput = JsonSerializer.Serialize(historyData);
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "demand_forecast.py");

                if (!System.IO.File.Exists(scriptPath)) return 0;

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(start))
                {
                    if (process == null) return 0;
                    using (StreamWriter writer = process.StandardInput) { await writer.WriteAsync(jsonInput); }
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = await reader.ReadToEndAsync();
                        if (decimal.TryParse(result, out decimal forecast)) return forecast;
                    }
                }
            }
            catch (Exception) { return 0; }
            return 0;
        }
    }
}
