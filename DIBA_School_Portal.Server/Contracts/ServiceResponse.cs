namespace DIBA_School_Portal.Server.Contracts
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public int StatusCode { get; set; }
        public List<T> Errors { get; set; }= new List<T>();


    }
}
