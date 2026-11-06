using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    public class CreditScoreRule : IEligibilityRule
    {
        public string RuleName => "Credit Score Validation";

        public int Order => 1;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context)
        {
            Console.WriteLine($"📋 Evaluating {RuleName}...");

            if(application.CreditScore < 500)
            {
                context.AddRejection($"Credit score {application.CreditScore} is too low. Minimum required: 580");
            }
            else if (application.CreditScore < 670)
            {
                context.AddWarning($"Credit score {application.CreditScore} is below preferred level. Expect higher interest rates");
            }

            context.SetData("CreditScore", application.CreditScore);

            return Task.CompletedTask;
        }
    }
}
