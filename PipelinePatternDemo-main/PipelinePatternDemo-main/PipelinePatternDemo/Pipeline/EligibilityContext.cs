namespace PipelinePatternDemo.Pipeline
{
    public class EligibilityContext
    {
        public Guid ApplicationId { get; }
        public List<string> Warnings { get; } = new();
        public List<string> RejectionReasons { get; } = new();
        public Dictionary<string, object> Data { get; } = new();

        public bool IsRejected => RejectionReasons.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public EligibilityContext(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
        public void AddWarning(string message)
        {
            Warnings.Add(message);
            Console.WriteLine($"⚠️  Warning: {message}");
        }
        public void AddRejection(string reason)
        {
            RejectionReasons.Add(reason);
            Console.WriteLine($"❌ Rejected: {reason}");
        }
        public void SetData(string key, object value)
        {
            Data[key] = value;
        }
        public T? GetData<T>(string key) where T : class
        {
            return Data.TryGetValue(key, out var value) 
                ? value as T 
                : null;
        }
    }
}
