namespace KrishiSahayAI.Models
{
    public class DashboardViewModel
    {
        public FarmProfile Farm { get; set; } = new();

        public List<FarmActivity> Activities { get; set; } = new();

        public int CompletedActivities { get; set; }

        public int TotalActivities { get; set; }

        public int ProgressPercentage
        {
            get
            {
                if (TotalActivities == 0)
                    return 0;

                return (int)Math.Round(
                    (double)CompletedActivities /
                    TotalActivities * 100);
            }
        }
    }
}