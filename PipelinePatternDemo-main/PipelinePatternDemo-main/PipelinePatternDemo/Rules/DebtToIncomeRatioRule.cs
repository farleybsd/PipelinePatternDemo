using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    public class DebtToIncomeRatioRule : IEligibilityRule
    {
        public string RuleName => "Debt-to-Income Ratio Check";

        public int Order => 4;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context)
        {
            Console.WriteLine($"📊 Evaluating {RuleName}...");

            var monthlyIncome = application.AnnualIncome / 12;
            var estimatedMonthlyPayment = (application.RequestedLoanAmount * 0.05m) / 12;
            var totalMonthlyDebt = application.MonthlyDebt + estimatedMonthlyPayment;
            var dtiRatio = totalMonthlyDebt / monthlyIncome;

            context.SetData("DTIRatio", dtiRatio);

            if (dtiRatio > 0.43m)
            {
                context.AddRejection(
                    $"Debt-to-Income ratio {dtiRatio:P2} exceeds maximum allowed 43%. " +
                    $"Monthly income: ${monthlyIncome:N2}, Total monthly debt: ${totalMonthlyDebt:N2}"
                );
            }
            else if (dtiRatio > 0.36m)
            {
                context.AddWarning($"DTI ratio {dtiRatio:P2} is elevated. Consider a smaller loan amount");
            }

            return Task.CompletedTask;
        }
    }
}
