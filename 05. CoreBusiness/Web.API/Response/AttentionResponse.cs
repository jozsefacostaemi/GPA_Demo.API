namespace Web.Core.Business.API.Response
{
    public class AttentionResponse
    {
        public Guid? AttentionId { get; set; }
        public string HealthCareStaff { get; set; }
        public string Patient { get; set; }
        public string Process { get; set; }
        public int Priority { get; set; }
        public string City { get; set; }
        public int Comorbities { get; set; }
        public string State { get; set; }
        public string Age { get; set; }
        public string Plan { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
