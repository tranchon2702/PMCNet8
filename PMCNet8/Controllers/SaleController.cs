using Data.Data;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PMCNet8.Models;
using System.Globalization;

public class SaleController : Controller
{
    private readonly Medihub4rumDbContext _mediHub4RumContext;
    private readonly MedihubSCAppDbContext _scAppContext;
    private readonly ILogger<SaleController> _logger;

    public SaleController(Medihub4rumDbContext mediHub4RumContext, ILogger<SaleController> logger, MedihubSCAppDbContext scAppContext)
    {
        _mediHub4RumContext = mediHub4RumContext;
        _logger = logger;
        _scAppContext = scAppContext;
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

            if (!string.IsNullOrEmpty(startDate))
            {
                parsedStartDate = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                parsedEndDate = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            else
            {
                parsedEndDate = DateTime.Now;
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

            // Thực hiện truy vấn dữ liệu cho bảng và biểu đồ
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
    public async Task<IActionResult> ExportSaleDataToExcel(string startDate = null, string endDate = null)
    {
        try
        {
            // Lấy sponsorId từ session
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            // Thiết lập ngày kết thúc mặc định là ngày hiện tại nếu không có endDate
            DateTime parsedEndDate = string.IsNullOrEmpty(endDate)
                ? DateTime.Now
                : DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            // Nếu không có ngày bắt đầu, sẽ lấy dữ liệu từ trước tới ngày kết thúc (endDate)
            DateTime? parsedStartDate = string.IsNullOrEmpty(startDate)
                ? (DateTime?)null
                : DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            // **Lấy dữ liệu quét QR với điều kiện sponsorId từ session**
            var scanStatistics = await _mediHub4RumContext.SponsorCampaignProductScan
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.ProductCode.Campaign.Product.Sponsor)
                .Where(s => s.ProductCode.Campaign.Product.SponsorId == sponsorId)  // Điều kiện lọc theo sponsorId từ session
                .Where(s => (parsedStartDate == null || s.ScanDate >= parsedStartDate) && s.ScanDate <= parsedEndDate)
                .OrderByDescending(s => s.ScanDate)
                .Select(s => new ListUserScanQRViewModel
                {
                    UserName = s.User.FullName,
                    PhoneNumber = s.User.Phone,
                    Province = s.User.KeyCodeActive,  // Sử dụng KeyCodeActive từ User
                    NameProduct = s.ProductCode.Campaign.Product.Name,
                    SponsorName = s.ProductCode.Campaign.Product.Sponsor.Name,
                    SponsorId = s.ProductCode.Campaign.Product.SponsorId,
                    Point = s.ProductCode.Campaign.Point,
                    ScanDate = s.ScanDate
                })
                .ToListAsync();

            // Lấy danh sách các mã KeyCodeActive từ User
            var keyCodeActives = scanStatistics.Select(e => e.Province).Distinct().ToList();

            // Lấy thông tin mã tỉnh từ AppSetup (nối với KeyCodeActive)
            var appSetups = await _scAppContext.AppSetup.AsNoTracking()
                .Where(e => keyCodeActives.Contains(e.KeyCodeActive))  // Lọc theo KeyCodeActive
                .Select(e => new { e.KeyCodeActive, e.ProvinceCode })
                .ToListAsync();

            // Lấy danh sách các mã ProvinceCode từ AppSetup
            var provinceCodes = appSetups.Select(e => e.ProvinceCode).Distinct().ToList();

            // Lấy tên tỉnh từ DmProvince (nối với ProvinceCode)
            var provinces = await _mediHub4RumContext.DmProvince.AsNoTracking()
                .Where(e => provinceCodes.Contains(e.Code))  // Lọc theo mã tỉnh
                .Select(e => new { e.Code, e.Name })
                .ToListAsync();

            // Kết hợp dữ liệu giữa scanStatistics và AppSetup
            var scansWithProvinceCode = (from scan in scanStatistics
                                         join app in appSetups on scan.Province equals app.KeyCodeActive
                                         select new ListUserScanQRViewModel
                                         {
                                             UserName = scan.UserName,
                                             PhoneNumber = scan.PhoneNumber,
                                             Province = app.ProvinceCode,  // Sử dụng ProvinceCode từ AppSetup
                                             NameProduct = scan.NameProduct,
                                             SponsorName = scan.SponsorName,
                                             SponsorId = scan.SponsorId,
                                             Point = scan.Point,
                                             ScanDate = scan.ScanDate
                                         }).ToList();

            // Nối ProvinceCode từ scansWithProvinceCode với tên tỉnh từ DmProvince
            var finalResult = (from scan in scansWithProvinceCode
                               join province in provinces on scan.Province equals province.Code into provinceJoin
                               from provinceMatch in provinceJoin.DefaultIfEmpty()  // Sử dụng DefaultIfEmpty để xử lý khi không có kết quả nối
                               select new ListUserScanQRViewModel
                               {
                                   UserName = scan.UserName,
                                   PhoneNumber = scan.PhoneNumber,
                                   Province = provinceMatch?.Name ?? "Không xác định",  // Nếu không tìm thấy tỉnh, hiển thị "Không xác định"
                                   NameProduct = scan.NameProduct,
                                   SponsorName = scan.SponsorName,
                                   SponsorId = scan.SponsorId,
                                   Point = scan.Point,
                                   ScanDate = scan.ScanDate
                               })
                 .OrderByDescending(x => x.ScanDate)
                 .ToList();

            // **Thiết lập ngữ cảnh giấy phép cho EPPlus**
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;  // Cấu hình giấy phép phi thương mại

            // Xuất Excel với định dạng đẹp hơn
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sales Data");

                // Thiết lập tiêu đề cho các cột
                worksheet.Cells[1, 1].Value = "Tên người dùng";
                worksheet.Cells[1, 2].Value = "Số điện thoại";
                worksheet.Cells[1, 3].Value = "Tỉnh/TP";
                worksheet.Cells[1, 4].Value = "Tên sản phẩm";
                worksheet.Cells[1, 5].Value = "Nhà tài trợ";
                worksheet.Cells[1, 6].Value = "Điểm";
                worksheet.Cells[1, 7].Value = "Ngày quét";

                // **Định dạng tiêu đề**: Làm đậm, căn giữa, màu nền
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Điền dữ liệu vào các ô và định dạng căn lề cho các ô dữ liệu
                for (int i = 0; i < finalResult.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = finalResult[i].UserName;
                    worksheet.Cells[i + 2, 2].Value = finalResult[i].PhoneNumber;
                    worksheet.Cells[i + 2, 3].Value = finalResult[i].Province;
                    worksheet.Cells[i + 2, 4].Value = finalResult[i].NameProduct;
                    worksheet.Cells[i + 2, 5].Value = finalResult[i].SponsorName;
                    worksheet.Cells[i + 2, 6].Value = finalResult[i].Point;
                    worksheet.Cells[i + 2, 7].Value = finalResult[i].ScanDate.ToString("dd/MM/yyyy HH:mm:ss");

                    // Định dạng căn lề cho cột ngày
                    worksheet.Cells[i + 2, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Cells[1, 1, finalResult.Count + 1, 7].AutoFitColumns();

                // Đặt border cho toàn bộ bảng
                using (var range = worksheet.Cells[1, 1, finalResult.Count + 1, 7])
                {
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Xuất ra file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"SaleData_{startDate ?? "start"}-{endDate ?? parsedEndDate.ToString("ddMMyyyy")}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(stream, contentType, fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while exporting sale data to Excel");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetChartData(string endDate, string groupBy, Guid? productId = null)
    {
        try
        {
            if (!Guid.TryParse(HttpContext.Session.GetString("SponsorId"), out Guid sponsorId))
            {
                return BadRequest("Invalid SponsorId");
            }

            DateTime parsedEndDate;
            if (!DateTime.TryParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedEndDate))
            {
                return BadRequest("Invalid end date format");
            }

            var query = from product in _mediHub4RumContext.SponsorProduct
                        join campaign in _mediHub4RumContext.SponsorCampaign on product.Id equals campaign.ProductId
                        join code in _mediHub4RumContext.SponsorCampaignProductCode on campaign.Id equals code.CampaignId
                        join scan in _mediHub4RumContext.SponsorCampaignProductScan on code.Code equals scan.Code
                        where product.SponsorId == sponsorId
                              && scan != null  
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
                            Count = result.Count(r => r.ScanDate.Date == date.Date) * 1000,
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
                            Count = result.Count(r => r.ScanDate.Date >= weekStart && r.ScanDate.Date < weekStart.AddDays(7)) * 1000,
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
                            Count = result.Count(r => r.ScanDate.Month == date.Month && r.ScanDate.Year == date.Year) * 1000,
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

