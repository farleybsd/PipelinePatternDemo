using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;

namespace PipelinePatternDemo.Rules
{
    public class EmploymentHistoryRule : IEligibilityRule
    {
        public string RuleName => "Employment History Check";

        public int Order => 3;

        public Task EvaluateAsync(LoanApplication application, EligibilityContext context)
        {
            Console.WriteLine($"💼 Evaluating {RuleName}...");

            if (application.EmploymentYears < 1)
            {
                context.AddRejection("Must be employed for at least 1 year");
            }
            else if(application.EmploymentYears < 2)
            {
                context.AddWarning("Less than 2 years of employment history. Application may require additional review");
            }

            context.SetData("EmploymentYears", application.EmploymentYears);
            
            return Task.CompletedTask;
        }
    }
}
