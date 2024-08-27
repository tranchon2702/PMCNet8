namespace PMCNet8.Models
{
    public class SponsorProductModel
    {
        public string TenSanPham { get; set; }
        public Guid ProductId { get;  set; }
        public string ProductName { get; internal set; }
        public int TotalScans { get;  set; }
        public int UniqueUsers { get;  set; }
        public int TotalPoints { get;  set; }
        public int AveragePointsPerScan { get;  set; }
    }
}
