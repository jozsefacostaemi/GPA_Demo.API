namespace Web.Core.Business.API.DTOs.Input
{
    public class RequestLoginDTO
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
