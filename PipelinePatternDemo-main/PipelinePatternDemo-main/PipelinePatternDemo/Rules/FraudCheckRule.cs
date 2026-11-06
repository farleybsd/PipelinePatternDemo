using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    public class FraudCheckRule : IEligibilityRule
    {
        public string RuleName => "Fraud Detection Check";

        public int Order => 5;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context)
        {
            Console.WriteLine($"🔍 Evaluating {RuleName}...");

            if (application.IsFlaggedForFraud)
            {
                context.AddRejection("Application has been flagged for potential fraud. Manual review required");
            }

            if (application.RequestedLoanAmount > application.AnnualIncome * 2)
            {
                context.AddWarning("Loan amount exceeds 2x annual income. Additional verification required");
            }

            return Task.CompletedTask;
        }
    }
}
