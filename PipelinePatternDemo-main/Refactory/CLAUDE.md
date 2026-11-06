# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build PipelinePatternDemo.slnx
dotnet run --project PipelinePatternDemo
```

```bash
dotnet test PipelinePatternDemo.slnx
dotnet run --project PipelinePatternDemo -- --logs   # console demo with structured logs
```

Solution (`PipelinePatternDemo.slnx`, XML format, .NET 10): core console/library `PipelinePatternDemo`, Minimal API `PipelinePatternDemo.Api`, and xUnit tests `PipelinePatternDemo.Tests` (rule boundary tests, pipeline tests with fake rules, API integration tests via `WebApplicationFactory<Program>`).

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
- **`EligibilityContext`** (`Pipeline/EligibilityContext.cs`) - Mutable state object shared across all rules. Tracks warnings, rejections, system errors and arbitrary key-value data. `IsRejected` drives pipeline short-circuit. It does no I/O (no console/logging).
- **`EligibilityPipeline`** (`Rules/EligibilityPipeline.cs`) - Orchestrates all rules sequentially. Stops on first rejection when `stopOnRejection = true`; a rule exception aborts the pipeline and yields `EligibilityStatus.PendingReview` (never Rejected). Logs only via `ILogger` (no PII). Also contains `EligibilityResult` class definition.
- **`LoanApplication`** (`Models/LoanApplication.cs`) - Input domain model.

### Rules (in `Rules/` directory)

Five rules registered in `Program.cs` via `services.AddScoped<IEligibilityRule, TRule>()`:
`CreditScoreRule` -> `IncomeValidationRule` -> `EmploymentHistoryRule` -> `DebtToIncomeRatioRule` -> `FraudCheckRule`

The pipeline sorts rules with `rules.OrderBy(r => r.Order)`, so execution follows each rule's `Order` property, not DI registration order.

User-facing strings and code comments are in Portuguese (pt-BR); identifiers (class/property names, JSON contract) stay in English. Design patterns are documented in `DESIGN_PATTERNS.md`.

### Notable patterns

- C# primary constructors used throughout
- DI via `Microsoft.Extensions.DependencyInjection` (not minimal API / ASP.NET Core host)
- `EligibilityResult` is nested in `EligibilityPipeline.cs` within the `Rules` namespace, not in a separate file or the Pipeline namespace

- Rules must not write to the console; presentation lives in the console `Program.cs` (`DisplayResult`) and the front-end.
- `IEligibilityRule.EvaluateAsync` takes a `CancellationToken`; API request validation uses DataAnnotations on `LoanEvaluationRequest` + `builder.Services.AddValidation()`.
