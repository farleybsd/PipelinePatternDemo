using PipelinePatternDemo.Models;

namespace PipelinePatternDemo.Pipeline
{
    public interface IEligibilityRule
    {
        string RuleName { get; }
        int Order { get; }
        Task EvaluateAsync(LoanApplication application, EligibilityContext context);
    }
}
