using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    public class IncomeValidationRule : IEligibilityRule
    {
        public string RuleName => "Income Validation";

        public int Order => 2;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context)
        {
            Console.WriteLine($"📋 Evaluating {RuleName}...");

            var minAnnualIncome = application.RequestedLoanAmount * 0.3m;

            if (application.AnnualIncome < minAnnualIncome)
            {
                context.AddRejection(
                    $"Annual income ${application.AnnualIncome:N2} is insufficient for loan amount ${application.RequestedLoanAmount:N2}. " +
                $"Minimum required: ${minAnnualIncome:N2}"
                    );
            }
            else if (application.AnnualIncome < minAnnualIncome * 1.5m)
            {
                context.AddWarning(
                    $"Income level is borderline for the requested loan amount"
                );
            }

            context.SetData("Income", minAnnualIncome);

            return Task.CompletedTask;
        }
    }
}
