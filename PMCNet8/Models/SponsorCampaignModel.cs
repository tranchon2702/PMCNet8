namespace PMCNet8.Models
{
    public class SponsorCampaignModel
    {
        public Guid Ma { get; set; }
        public string Ten { get; set; }

        public int SLuong { get; set; }
        public int Diem { get; set; }
        public Guid MSPham { get; set; }

        public string Ten_SPham { get; set; }
        public string Ten_NTT { get; set; }

        public DateTime NTao { get; set; }
    }
}
