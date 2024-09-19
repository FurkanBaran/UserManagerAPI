namespace UserManager.DTOs.APIResponses
{
    public class ErrorResponse
    {
        public int HttpStatusCode { get; set; }
        public string? ErrorMessages { get; set; }
        public bool IsError { get; set; }
    }

}
