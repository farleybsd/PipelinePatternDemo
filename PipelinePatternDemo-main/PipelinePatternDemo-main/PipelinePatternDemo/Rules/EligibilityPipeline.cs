using Microsoft.Extensions.Logging;
using PipelinePatternDemo.Models;
using PipelinePatternDemo.Pipeline;
using System.Data;

namespace PipelinePatternDemo.Rules
{
    public class EligibilityPipeline(
        IEnumerable<IEligibilityRule> rules,
        ILogger<EligibilityPipeline> logger
        )
    {
        public async Task<EligibilityResult> ExecuteAsync(LoanApplication application)
        {
            var context = new EligibilityContext(application.ApplicationId);

            logger.LogInformation(
            "Starting eligibility pipeline for Application {ApplicationId}",
            application.ApplicationId);

            Console.WriteLine($"\n🚀 Starting Loan Eligibility Pipeline for {application.ApplicantName}");
            Console.WriteLine($"Application ID: {application.ApplicationId}");
            Console.WriteLine(new string('-', 60));

            var stopOnRejection = true;

            foreach (var rule in rules)
            {
                try
                {
                    await rule.EvaluateAsync(application, context);

                    // Optionally stop pipeline on first rejection
                    if (stopOnRejection && context.IsRejected)
                    {
                        logger.LogWarning(
                            "Application {ApplicationId} rejected by rule: {RuleName}",
                            application.ApplicationId,
                            rule.RuleName);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing rule {RuleName}", rule.RuleName);
                    context.AddRejection($"System error during {rule.RuleName}");
                }
            }

            Console.WriteLine(new string('-', 60));

            var result = new EligibilityResult
            {
                ApplicationId = application.ApplicationId,
                IsApproved = !context.IsRejected,
                RejectionReasons = context.RejectionReasons,
                Warnings = context.Warnings,
                ContextData = context.Data
            };

            DisplayResult(result);

            return result;
        }

        private void DisplayResult(EligibilityResult result)
        {
            if (result.IsApproved)
            {
                Console.WriteLine($"✅ APPLICATION APPROVED for {result.ApplicationId}");

                if ((result.Warnings.Any()))
                {
                    Console.WriteLine($"\n⚠️  With {result.Warnings.Count} warning(s):");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"   - {warning}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ APPLICATION REJECTED for {result.ApplicationId}");
                    Console.WriteLine($"\nRejection Reasons:");
                    foreach (var reason in result.RejectionReasons)
                    {
                        Console.WriteLine($"   - {reason}");
                    }
                }
            }
        }
    }

    public class EligibilityResult
    {
        public Guid ApplicationId { get; set; }
        public bool IsApproved { get; set; }
        public List<string> RejectionReasons { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> ContextData { get; set; } = new();
    }
}
