# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build PipelinePatternDemo.slnx
dotnet run --project PipelinePatternDemo
```

No test project exists. Single project solution targeting .NET 10 with `PipelinePatternDemo.slnx` (XML-based solution format).

## Architecture

This is a Pipeline Pattern demo for loan eligibility evaluation. The execution flow:

```
Program.cs (DI setup + demo data)
  -> LoanEligibilityService
    -> EligibilityPipeline
      -> IEligibilityRule implementations (registered via DI, executed in foreach)
        -> EligibilityContext (accumulates warnings/rejections)
```

### Key abstractions

- **`IEligibilityRule`** (`Pipeline/IEligibilityRule.cs`) - Rule interface with `RuleName`, `Order`, and `EvaluateAsync(application, context)`. Each rule evaluates a loan application and writes results to the shared context.
- **`EligibilityContext`** (`Pipeline/EligibilityContext.cs`) - Mutable state object shared across all rules. Tracks warnings, rejections, and arbitrary key-value data. `IsRejected` property drives pipeline short-circuit.
- **`EligibilityPipeline`** (`Rules/EligibilityPipeline.cs`) - Orchestrates all rules sequentially. Stops on first rejection when `stopOnRejection = true`. Also contains `EligibilityResult` class definition.
- **`LoanApplication`** (`Models/LoanApplication.cs`) - Input domain model.

### Rules (in `Rules/` directory)

Five rules registered in `Program.cs` via `services.AddScoped<IEligibilityRule, TRule>()`:
`CreditScoreRule` -> `IncomeValidationRule` -> `EmploymentHistoryRule` -> `DebtToIncomeRatioRule` -> `FraudCheckRule`

Rules use `Order` property but are not explicitly sorted - execution follows DI registration order.

### Notable patterns

- C# primary constructors used throughout
- DI via `Microsoft.Extensions.DependencyInjection` (not minimal API / ASP.NET Core host)
- `EligibilityResult` is nested in `EligibilityPipeline.cs` within the `Rules` namespace, not in a separate file or the Pipeline namespace
