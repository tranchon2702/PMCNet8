using System.Diagnostics.CodeAnalysis;

namespace PMCNet8.Models
{
    public class UnregisteredPharmacistViewModel
    {
        public string? TenDuocSi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DonViCongTac { get; set; }
        public string? DiaChi { get; set; }
        public bool DaXacThuc { get; set; }

        [AllowNull]
        public string? KeyCodeActive { get; set; }
        public DateTime NgayHoanThanh{ get; set; }
    }
}
