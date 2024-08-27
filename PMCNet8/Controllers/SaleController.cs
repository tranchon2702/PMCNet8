using Data.Data;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCNet8.Models;
using System.Globalization;

public class SaleController : Controller
{

    private readonly LogActionDbContext _logActionContext;
    private readonly Medihub4rumDbContext _mediHub4RumContext;
    private readonly MedihubSCAppDbContext _mediHubSCAppContext;
    private readonly ILogger<SaleController> _logger;
    public SaleController(LogActionDbContext logActionContext, Medihub4rumDbContext mediHub4RumContext, MedihubSCAppDbContext mediHubSCAppContext, ILogger<SaleController> logger)
    {
        _logActionContext = logActionContext;
        _mediHub4RumContext = mediHub4RumContext;
        _mediHubSCAppContext = mediHubSCAppContext;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new SaleViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetSaleData(Guid sponsorId, string startDate, string endDate)
    {
        try
        {
            DateTime parsedStartDate = string.IsNullOrEmpty(startDate) ? DateTime.Today.AddDays(-30) : DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime parsedEndDate = string.IsNullOrEmpty(endDate) ? DateTime.Today : DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var result = await _mediHub4RumContext.SponsorProduct
                     .Where(sp => sp.SponsorId == sponsorId)
                     .Include(sp => sp.Campaign)
                         .ThenInclude(c => c.ProductCodes)
                             .ThenInclude(pc => pc.ProductScan)
                     .Select(sp => new SponsorProductModel
                     {
                         ProductId = sp.Id,
                         ProductName = sp.Name,
                         TotalScans = sp.Campaign.ProductCodes
                             .Count(pc => pc.ProductScan != null &&
                                          pc.ProductScan.ScanDate >= parsedStartDate &&
                                          pc.ProductScan.ScanDate <= parsedEndDate),
                         UniqueUsers = sp.Campaign.ProductCodes
                             .Where(pc => pc.ProductScan != null &&
                                          pc.ProductScan.ScanDate >= parsedStartDate &&
                                          pc.ProductScan.ScanDate <= parsedEndDate)
                             .Select(pc => pc.ProductScan.UserId)
                             .Distinct()
                             .Count(),
                         TotalPoints = sp.Campaign.ProductCodes
                             .Count(pc => pc.ProductScan != null &&
                                          pc.ProductScan.ScanDate >= parsedStartDate &&
                                          pc.ProductScan.ScanDate <= parsedEndDate) * sp.Campaign.Point,
                         AveragePointsPerScan = sp.Campaign.Point
                     })
                     .ToListAsync();

            var model = new SaleViewModel
            {
                StartDate = parsedStartDate,
                EndDate = parsedEndDate,
                SponsorProducts = result,
                CustomerCount = result.Sum(x => x.UniqueUsers),
                ProductCount = result.Sum(x => x.TotalScans),
                TotalAmount = result.Sum(x => x.TotalPoints),
                AveragePerProduct = result.Sum(x => x.TotalPoints) / result.Sum(x => x.TotalScans)
            };

            return PartialView("_Sale", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching sponsor product activity data");
            return StatusCode(500, "An error occurred while processing your request");
        }


    }
}
