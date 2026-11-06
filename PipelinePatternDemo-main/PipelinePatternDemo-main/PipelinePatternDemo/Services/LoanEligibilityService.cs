using PipelinePatternDemo.Models;
using PipelinePatternDemo.Rules;

namespace PipelinePatternDemo.Services
{
    public class LoanEligibilityService(EligibilityPipeline pipeline)
    {
        public async Task<EligibilityResult> EvaluateApplicationAsync(LoanApplication application)
        {
            Console.WriteLine("\n📝 Loan Eligibility Service - Processing Application");

            return await pipeline.ExecuteAsync(application);
        }
    }
}
