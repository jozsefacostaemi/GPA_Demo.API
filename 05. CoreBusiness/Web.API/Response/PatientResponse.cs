namespace Web.Core.Business.API.Response
{
    public class PatientResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Identification { get; set; }
        public string? Age { get; set; }
    }
}
