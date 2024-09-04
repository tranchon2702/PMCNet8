using Data.Data;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCNet8.Models;
using System.Globalization;

public class SaleController : Controller
{
    private readonly Medihub4rumDbContext _mediHub4RumContext;
    private readonly ILogger<SaleController> _logger;

    public SaleController(Medihub4rumDbContext mediHub4RumContext, ILogger<SaleController> logger)
    {
        _mediHub4RumContext = mediHub4RumContext;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            var products = await _mediHub4RumContext.SponsorProduct
                .Where(sp => sp.SponsorId == sponsorId)
                .Select(sp => new { sp.Id, sp.Name })
                .ToListAsync();

            return Json(new { products });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching products");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetSaleData(string startDate = null, string endDate = null)
    {
        try
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                parsedStartDate = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                parsedEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            // Kiểm tra xem sponsor có sản phẩm nào không
            var hasProducts = await _mediHub4RumContext.SponsorProduct
                .AnyAsync(sp => sp.SponsorId == sponsorId);

            if (!hasProducts)
            {
                return PartialView("_Sale", new SaleViewModel
                {
                    SponsorId = sponsorId,
                    StartDate = parsedStartDate,
                    EndDate = parsedEndDate,
                    NoDataMessage = "Chưa có sản phẩm nào được quét."
                });
            }

            var query = from product in _mediHub4RumContext.SponsorProduct
                        where product.SponsorId == sponsorId
                        join campaign in _mediHub4RumContext.SponsorCampaign on product.Id equals campaign.ProductId into campaignGroup
                        from campaign in campaignGroup.DefaultIfEmpty()
                        join code in _mediHub4RumContext.SponsorCampaignProductCode on campaign.Id equals code.CampaignId into codeGroup
                        from code in codeGroup.DefaultIfEmpty()
                        join scan in _mediHub4RumContext.SponsorCampaignProductScan on code.Code equals scan.Code into scanGroup
                        from scan in scanGroup.DefaultIfEmpty()
                        where (parsedStartDate == null || parsedEndDate == null) ||
                              (scan == null || (scan.ScanDate.Date >= parsedStartDate.Value.Date && scan.ScanDate.Date <= parsedEndDate.Value.Date))
                        group new { product, campaign, scan } by new { product.Id, product.Name, campaign.Point } into g
                        select new SponsorProductModel
                        {
                            ProductId = g.Key.Id,
                            ProductName = g.Key.Name,
                            TotalScans = g.Count(x => x.scan != null),
                            UniqueUsers = g.Where(x => x.scan != null).Select(x => x.scan.UserId).Distinct().Count(),
                            PointPerScan = g.Key.Point,
                            TotalPoints = g.Count(x => x.scan != null) * (g.Key.Point)
                        };

            var result = await query.ToListAsync();

            var model = new SaleViewModel
            {
                SponsorId = sponsorId,
                StartDate = parsedStartDate,
                EndDate = parsedEndDate,
                SponsorProducts = result,
                CustomerCount = result.Sum(x => x.UniqueUsers),
                ProductCount = result.Sum(x => x.TotalScans),
                TotalAmount = result.Sum(x => x.TotalPoints),
                AveragePerProduct = result.Sum(x => x.TotalScans) > 0 ?
                    (double)result.Sum(x => x.TotalPoints) / result.Sum(x => x.TotalScans) : 0
            };

            if (!result.Any(x => x.TotalScans > 0))
            {
                model.NoDataMessage = "Không có dữ liệu bán hàng trong khoảng thời gian này.";
            }

            return PartialView("_Sale", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching sponsor product activity data");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(string endDate, string groupBy, Guid? productId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(endDate))
            {
                return BadRequest("Vui lòng chọn thời gian đến ngày");
            }

            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            if (!DateTime.TryParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                return BadRequest("Invalid end date format");
            }

            var query = from product in _mediHub4RumContext.SponsorProduct
                        join campaign in _mediHub4RumContext.SponsorCampaign on product.Id equals campaign.ProductId
                        join code in _mediHub4RumContext.SponsorCampaignProductCode on campaign.Id equals code.CampaignId
                        join scan in _mediHub4RumContext.SponsorCampaignProductScan on code.Code equals scan.Code
                        where product.SponsorId == sponsorId
                        select new { product.Id, product.Name, scan.ScanDate, Points = campaign.Point };

            if (productId.HasValue)
            {
                query = query.Where(q => q.Id == productId.Value);
            }

            var result = await query.ToListAsync();

            if (!result.Any())
            {
                return Json(new { chartData = new List<object>(), productName = "Không có dữ liệu" });
            }

            List<object> chartData;
            DateTime startDate;

            switch (groupBy)
            {
                case "day":
                    startDate = parsedEndDate.AddDays(-6);
                    chartData = Enumerable.Range(0, 7)
                        .Select(offset => startDate.AddDays(offset))
                        .Select(date => new
                        {
                            Label = date.ToString("dd/MM/yyyy"),
                            Count = result.Count(r => r.ScanDate.Date == date.Date),
                            Points = result.Where(r => r.ScanDate.Date == date.Date).Sum(r => r.Points)
                        })
                        .Cast<object>()
                        .ToList();
                    break;

                case "week":
                    startDate = parsedEndDate.AddDays(-(int)parsedEndDate.DayOfWeek + 1).AddDays(-21);
                    chartData = Enumerable.Range(0, 4)
                        .Select(weekOffset => startDate.AddDays(weekOffset * 7))
                        .Select(weekStart => new
                        {
                            Label = $"Tuần {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}",
                            Count = result.Count(r => r.ScanDate.Date >= weekStart && r.ScanDate.Date < weekStart.AddDays(7)),
                            Points = result.Where(r => r.ScanDate.Date >= weekStart && r.ScanDate.Date < weekStart.AddDays(7)).Sum(r => r.Points)
                        })
                        .Cast<object>()
                        .ToList();
                    break;

                case "month":
                    startDate = parsedEndDate.AddMonths(-2);
                    chartData = Enumerable.Range(0, 3)
                        .Select(i => startDate.AddMonths(i))
                        .Select(date => new
                        {
                            Label = date.ToString("MM/yyyy"),
                            Count = result.Count(r => r.ScanDate.Month == date.Month && r.ScanDate.Year == date.Year),
                            Points = result.Where(r => r.ScanDate.Month == date.Month && r.ScanDate.Year == date.Year).Sum(r => r.Points)
                        })
                        .Cast<object>()
                        .ToList();
                    break;

                default:
                    return BadRequest("Invalid groupBy parameter");
            }

            string productName = productId.HasValue
                ? result.FirstOrDefault()?.Name ?? "Sản phẩm không xác định"
                : "Tất cả sản phẩm";

            return Json(new { chartData, productName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching chart data");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}

