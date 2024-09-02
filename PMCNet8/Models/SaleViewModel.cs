namespace PMCNet8.Models
{
    public class SaleViewModel
    {
        public Guid SponsorId { get; set; }
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int TotalAmount { get; set; }
        public double AveragePerProduct { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SponsorProductModel> SponsorProducts { get; set; }
        public string NoDataMessage { get;  set; }
    }
}
