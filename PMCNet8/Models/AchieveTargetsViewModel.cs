namespace PMCNet8.Models
{
    public class AchieveTargetsViewModel
    {
        public DateTime? TargetStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public int TargetFinish { get; set; }
        public int TargetJoin { get; set; }
        public Dictionary<DateTime, int> TotalFinishs { get; set; }
        public Dictionary<DateTime, int> TotalEnters { get; set; }
        public Dictionary<DateTime, int> TotalPassed { get;  set; }
        public Dictionary<DateTime, int> TotalWatchedAllVideos { get; set; }

        public Dictionary<DateTime, int> TotalJoins { get; set; }
        public DateTime? CurrentDate { get; set; }
    }
}
