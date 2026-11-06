namespace PipelinePatternDemo.Pipeline
{
    // O contexto funciona como um "caderno de anotações" que passa de mão em mão entre as regras.
    // Cada regra escreve aqui o que encontrou: um aviso, um motivo de reprovação ou algum dado útil
    // para as regras seguintes. No final, o pipeline lê esse caderno para montar o resultado.
    //
    // Repare que o contexto não escreve nada no console nem em log: ele só guarda informação.
    // Quem decide como e onde mostrar isso é a camada de cima (pipeline, console ou API).
    public class EligibilityContext
    {
        // A proposta à qual estas anotações pertencem.
        public Guid ApplicationId { get; }

        // Pontos de atenção que não impedem a aprovação, mas merecem cuidado.
        public List<string> Warnings { get; } = new();

        // Motivos de negócio que fazem a proposta ser reprovada.
        public List<string> RejectionReasons { get; } = new();

        // Falhas técnicas (exceções) que impediram alguma regra de terminar.
        // Ficam separadas das reprovações para não confundirmos "não pode" com "não deu para saber".
        public List<string> SystemErrors { get; } = new();

        // Espaço livre para as regras compartilharem informações entre si (ex.: o índice DTI calculado).
        public Dictionary<string, object> Data { get; } = new();

        // Basta um único motivo de reprovação para a proposta ser considerada reprovada.
        public bool IsRejected => RejectionReasons.Count > 0;

        // Atalho para saber se existe algum aviso registrado.
        public bool HasWarnings => Warnings.Count > 0;

        // Indica que a análise ficou incompleta por algum problema técnico.
        public bool HasSystemErrors => SystemErrors.Count > 0;

        public EligibilityContext(Guid applicationId)
        {
            ApplicationId = applicationId;
        }

        // Registra um ponto de atenção.
        public void AddWarning(string message) => Warnings.Add(message);

        // Registra um motivo de reprovação.
        public void AddRejection(string reason) => RejectionReasons.Add(reason);

        // Registra que uma regra não conseguiu ser executada.
        public void AddSystemError(string message) => SystemErrors.Add(message);

        // Guarda (ou sobrescreve) um dado qualquer para ser usado por outras regras.
        public void SetData(string key, object value)
        {
            Data[key] = value;
        }

        // Recupera um dado guardado anteriormente; se não existir ou for de outro tipo, devolve null.
        public T? GetData<T>(string key) where T : class
        {
            return Data.TryGetValue(key, out var value)
                ? value as T
                : null;
        }
    }
}
