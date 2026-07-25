namespace TimeTracker.Responses
{
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}
