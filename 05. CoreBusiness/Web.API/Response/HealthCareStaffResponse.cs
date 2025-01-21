namespace Web.Core.Business.API.Response
{
    public class HealthCareStaffResponse
    {
        public Guid? ActualStateId { get; set; }
        public string? ActualStateDesc { get; set; }
        public string? ActualStateCode { get; set; }
        public Guid? HealthCareStaffId { get; set; }
        public string? HealthCareStaffName { get; set; }
    }
}
