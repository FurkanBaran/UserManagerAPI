namespace UserManager.DTOs.APIResponses
{
    public class ApiResponse<T>
    {
        public bool Result { get; set; }
        public T Data { get; set; } = default!;
        public ErrorResponse Error { get; set; } = new ErrorResponse();
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public bool IsError { get; set; }
    }
}
