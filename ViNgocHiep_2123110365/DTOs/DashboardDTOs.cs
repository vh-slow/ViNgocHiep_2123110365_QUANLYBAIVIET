namespace ViNgocHiep_2123110365.DTOs
{
    public class ChartDataDTO
    {
        public string Date { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class AdminAnalyticsDTO
    {
        public List<ChartDataDTO> ViewsChart { get; set; } = new();
        public List<ChartDataDTO> UsersChart { get; set; } = new();
    }
}
