namespace OrderRequestQueueProcessor.Models
{
    public class ProcessResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static ProcessResult Success() => new ProcessResult { IsSuccess = true };
        
        public static ProcessResult Failure(string message) => new ProcessResult { IsSuccess = false, ErrorMessage = message };
    }
}
