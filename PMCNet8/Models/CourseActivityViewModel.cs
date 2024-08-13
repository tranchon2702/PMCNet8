

namespace PMCNet8.Models
    {
        public class CourseActivityViewModel
        {
            public string? DonViCongTac { get; set; }
            public string SoDienThoai {  get; set; }
            public string? Email { get; set; }
            public string? DiaChi { get; set; }
            public string? TenDuocSi { get; set; }
            public string? KetQua { get; set; }
            public string? CourseName { get; set; }
            public List<ChartDataViewModel> ChartData { get; set; }
            public List<CourseActivityViewModel> TableData { get; set; }
            public DateTime StartDate { get;  set; }
            public DateTime EndDate { get;  set; }
    }
    }
