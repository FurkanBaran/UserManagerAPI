namespace UserManager.DTOs.ServiceResponses
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; } = true;
        public List<string> Messages { get; set; } = new List<string>();
        public T Data { get; set; }= default!;
    }
}
