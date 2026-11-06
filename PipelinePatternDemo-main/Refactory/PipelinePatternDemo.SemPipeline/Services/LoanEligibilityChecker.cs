using PipelinePatternDemo.SemPipeline.Models;

namespace PipelinePatternDemo.SemPipeline.Services
{
    // Este é o jeito "tradicional" de resolver o problema: uma classe só, com um método só,
    // que faz todas as verificações uma atrás da outra com if/else.
    //
    // Funciona? Funciona. A saída é igual à do projeto com Pipeline.
    // O problema aparece quando o sistema cresce. Os comentários marcados com "⚠️ DOR"
    // mostram onde isso começa a doer no dia a dia de um time.
    public class LoanEligibilityChecker
    {
        // ⚠️ DOR: todos os limites de todas as regras ficam misturados no topo da mesma classe.
        // Quem mexe na política de fraude abre o mesmo arquivo de quem mexe na política de score.
        private const int MinimumCreditScore = 580;
        private const int PreferredCreditScore = 670;
        private const decimal MinIncomeToLoanRatio = 0.3m;
        private const double MinimumEmploymentYears = 1;
        private const double PreferredEmploymentYears = 2;
        private const decimal MaxDtiRatio = 0.43m;
        private const decimal WarningDtiRatio = 0.36m;

        public LoanDecision Evaluate(LoanApplication application)
        {
            var decision = new LoanDecision { ApplicationId = application.ApplicationId };

            // ─────────────────────────────────────────────────────────────
            // 1) Score de crédito
            // ─────────────────────────────────────────────────────────────
            if (application.CreditScore < MinimumCreditScore)
            {
                decision.RejectionReasons.Add($"Score de crédito {application.CreditScore} é muito baixo. Mínimo exigido: {MinimumCreditScore}");

                // ⚠️ DOR: o "parar na primeira reprovação" vira um return espalhado em cada bloco.
                // Se alguém esquecer um desses returns, o comportamento muda sem ninguém perceber.
                return decision;
            }
            else if (application.CreditScore < PreferredCreditScore)
            {
                decision.Warnings.Add($"Score de crédito {application.CreditScore} está abaixo do nível desejado. Espere taxas de juros mais altas");
            }

            // ─────────────────────────────────────────────────────────────
            // 2) Renda
            // ─────────────────────────────────────────────────────────────
            var minAnnualIncome = application.RequestedLoanAmount * MinIncomeToLoanRatio;

            if (application.AnnualIncome < minAnnualIncome)
            {
                decision.RejectionReasons.Add(
                    $"Renda anual de ${application.AnnualIncome:N2} é insuficiente para um empréstimo de ${application.RequestedLoanAmount:N2}. " +
                    $"Mínimo exigido: ${minAnnualIncome:N2}");
                return decision;
            }
            else if (application.AnnualIncome < minAnnualIncome * 1.5m)
            {
                decision.Warnings.Add("Renda está no limite para o valor de empréstimo solicitado");
            }

            // ─────────────────────────────────────────────────────────────
            // 3) Histórico de emprego
            // ─────────────────────────────────────────────────────────────
            if (application.EmploymentYears < MinimumEmploymentYears)
            {
                decision.RejectionReasons.Add("É necessário estar empregado há pelo menos 1 ano");
                return decision;
            }
            else if (application.EmploymentYears < PreferredEmploymentYears)
            {
                decision.Warnings.Add("Menos de 2 anos de histórico de emprego. A proposta pode precisar de análise adicional");
            }

            // ─────────────────────────────────────────────────────────────
            // 4) Índice de endividamento (DTI)
            // ─────────────────────────────────────────────────────────────
            // ⚠️ DOR: para testar só o cálculo do DTI, o teste precisa montar uma proposta que
            // passe por score, renda e emprego primeiro. Não dá para testar esta etapa sozinha.
            if (application.AnnualIncome <= 0)
            {
                decision.RejectionReasons.Add("Não é possível calcular o índice de endividamento sem renda anual informada");
                return decision;
            }

            var monthlyIncome = application.AnnualIncome / 12;
            var estimatedMonthlyPayment = (application.RequestedLoanAmount * 0.05m) / 12;
            var totalMonthlyDebt = application.MonthlyDebt + estimatedMonthlyPayment;
            var dtiRatio = totalMonthlyDebt / monthlyIncome;

            if (dtiRatio > MaxDtiRatio)
            {
                decision.RejectionReasons.Add(
                    $"Índice de endividamento de {dtiRatio:P2} ultrapassa o máximo permitido de 43%. " +
                    $"Renda mensal: ${monthlyIncome:N2}, Dívida mensal total: ${totalMonthlyDebt:N2}");
                return decision;
            }
            else if (dtiRatio > WarningDtiRatio)
            {
                decision.Warnings.Add($"Índice de endividamento de {dtiRatio:P2} está elevado. Considere um valor de empréstimo menor");
            }

            // ─────────────────────────────────────────────────────────────
            // 5) Fraude
            // ─────────────────────────────────────────────────────────────
            // ⚠️ DOR: quer rodar a checagem de fraude PRIMEIRO (ela é mais barata e mais grave)?
            // No Pipeline bastaria mudar o Order. Aqui é preciso recortar e colar este bloco
            // lá para cima, tomando cuidado com as variáveis que os blocos anteriores criaram.
            if (application.IsFlaggedForFraud)
            {
                decision.RejectionReasons.Add("A proposta foi sinalizada como possível fraude. Revisão manual necessária");
            }

            if (application.RequestedLoanAmount > application.AnnualIncome * 2)
            {
                decision.Warnings.Add("Valor do empréstimo ultrapassa 2x a renda anual. Verificação adicional necessária");
            }

            // ⚠️ DOR: uma regra nova (ex.: consulta ao Serasa) entra aqui, deixando este método
            // ainda maior. Toda regra nova obriga a editar e retestar esta classe inteira,
            // o que viola o princípio Aberto/Fechado (OCP).
            //
            // ⚠️ DOR: não existe tratamento de erro por etapa. Se uma regra futura chamar um serviço
            // externo e ele cair, a exceção derruba a análise inteira — ou alguém espalha try/catch
            // em cada bloco.
            //
            // ⚠️ DOR: não dá para ligar/desligar uma regra por produto ou ambiente sem colocar
            // mais um if em volta do bloco.

            return decision;
        }
    }
}
