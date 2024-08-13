namespace PMCNet8.Models
{
    public class AchieveTargetsViewModel
    {
        public DateTime? TargetStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }
        public int TargetFinish { get; set; }
        public int TargetJoin { get; set; }
        public int TotalBeforeNow { get; set; }
        public int TotalDay { get; set; }
        public int FinishBeforeNow { get; set; }
        public int FinishDay { get; set; }
    }
}
