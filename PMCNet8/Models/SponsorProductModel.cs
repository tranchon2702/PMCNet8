namespace PMCNet8.Models
{
    public class SponsorProductModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalScans { get; set; }
        public int UniqueUsers { get; set; }
        public int PointPerScan { get; set; }
        public int TotalPoints { get; set; }
    }
}
