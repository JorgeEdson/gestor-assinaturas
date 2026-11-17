namespace GestorAssinaturas.Dominio.Comum;

public class Resultado
{
    protected Resultado(IEnumerable<string>? erros, TipoDeFalha tipoDeFalha)
    {
        Erros = erros?.ToList();
        TipoDeFalha = EhSucesso ? TipoDeFalha.Nenhuma : tipoDeFalha;
    }

    public IEnumerable<string>? Erros { get; }

    public TipoDeFalha TipoDeFalha { get; }

    public bool EhSucesso => Erros is null || !Erros.Any();

    public bool EhFalha => !EhSucesso;

    public bool EhRecursoNaoEncontrado => TipoDeFalha == TipoDeFalha.RecursoNaoEncontrado;

    public static Resultado Sucesso()
    {
        return new Resultado(null, TipoDeFalha.Nenhuma);
    }

    public static Resultado Falha(IEnumerable<string> erros)
    {
        return new Resultado(erros, TipoDeFalha.ViolacaoDeRegraDeNegocio);
    }

    public static Resultado Falha(string erro)
    {
        return new Resultado(new List<string> { erro }, TipoDeFalha.ViolacaoDeRegraDeNegocio);
    }

    public static Resultado NaoEncontrado(string erro)
    {
        return new Resultado(new List<string> { erro }, TipoDeFalha.RecursoNaoEncontrado);
    }

    public static Resultado FalhaAPartirDe(Resultado resultadoComFalha)
    {
        ArgumentNullException.ThrowIfNull(resultadoComFalha);

        if (resultadoComFalha.EhSucesso)
        {
            throw new InvalidOperationException("Não é possível propagar a falha de um resultado de sucesso.");
        }

        return new Resultado(resultadoComFalha.Erros, resultadoComFalha.TipoDeFalha);
    }

    public static Resultado FalhaQuando(bool condicaoDeViolacao, string erro)
    {
        return condicaoDeViolacao ? Falha(erro) : Sucesso();
    }

    public static Resultado Combinar(params Resultado[] resultados)
    {
        var erros = new List<string>();

        foreach (var resultado in resultados)
        {
            if (resultado.EhFalha)
            {
                erros.AddRange(resultado.Erros!);
            }
        }

        return erros.Count > 0 ? Falha(erros) : Sucesso();
    }
}

public class Resultado<T> : Resultado
{
    private readonly T? _instancia;

    private Resultado(T? instancia, IEnumerable<string>? erros, TipoDeFalha tipoDeFalha)
        : base(erros, tipoDeFalha)
    {
        _instancia = instancia;
    }

    public T Instancia
    {
        get
        {
            if (EhFalha)
            {
                throw new InvalidOperationException("Não é possível acessar a instância de um resultado com falha.");
            }

            return _instancia!;
        }
    }

    public static Resultado<T> Sucesso(T instancia)
    {
        return new Resultado<T>(instancia, null, TipoDeFalha.Nenhuma);
    }

    public static new Resultado<T> Falha(string erro)
    {
        return new Resultado<T>(default, new List<string> { erro }, TipoDeFalha.ViolacaoDeRegraDeNegocio);
    }

    public static new Resultado<T> Falha(IEnumerable<string> erros)
    {
        return new Resultado<T>(default, erros, TipoDeFalha.ViolacaoDeRegraDeNegocio);
    }

    public static new Resultado<T> NaoEncontrado(string erro)
    {
        return new Resultado<T>(default, new List<string> { erro }, TipoDeFalha.RecursoNaoEncontrado);
    }

    public static new Resultado<T> FalhaAPartirDe(Resultado resultadoComFalha)
    {
        ArgumentNullException.ThrowIfNull(resultadoComFalha);

        if (resultadoComFalha.EhSucesso)
        {
            throw new InvalidOperationException("Não é possível propagar a falha de um resultado de sucesso.");
        }

        return new Resultado<T>(default, resultadoComFalha.Erros, resultadoComFalha.TipoDeFalha);
    }
}
