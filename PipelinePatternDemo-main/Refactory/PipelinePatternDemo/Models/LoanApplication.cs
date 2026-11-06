namespace PipelinePatternDemo.Models
{
    // Esta é a "ficha" do cliente que chega pedindo um empréstimo.
    // Pense nela como o formulário preenchido no balcão do banco: guarda só os dados,
    // sem nenhuma regra de negócio. Quem decide se o pedido passa ou não são as regras do pipeline.
    public class LoanApplication
    {
        // Identificador único da proposta, gerado automaticamente para não precisarmos nos preocupar com isso.
        public Guid ApplicationId { get; set; } = Guid.NewGuid();

        // Nome de quem está pedindo o empréstimo.
        public string ApplicantName { get; set; } = string.Empty;

        // Pontuação de crédito (score), normalmente entre 300 e 850.
        public int CreditScore { get; set; }

        // Renda anual bruta do cliente.
        public decimal AnnualIncome { get; set; }

        // Quanto o cliente já paga de dívidas por mês.
        public decimal MonthlyDebt { get; set; }

        // Valor que o cliente quer pegar emprestado.
        public decimal RequestedLoanAmount { get; set; }

        // Há quantos anos o cliente está empregado (aceita frações, ex.: 1.5).
        public double EmploymentYears { get; set; }

        // Indica se o cliente já tem outros empréstimos em aberto.
        public bool HasExistingLoans { get; set; }

        // Sinal vindo de algum sistema antifraude dizendo que essa proposta é suspeita.
        public bool IsFlaggedForFraud { get; set; }

        // Data em que a proposta foi registrada (em UTC, para evitar confusão com fuso horário).
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    }
}
