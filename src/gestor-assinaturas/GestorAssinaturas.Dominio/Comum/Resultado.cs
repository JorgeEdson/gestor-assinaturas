namespace GestorAssinaturas.Dominio.Comum;

public class Resultado
{
    protected Resultado(IEnumerable<string>? erros = null)
    {
        Erros = erros?.ToList();
    }

    public IEnumerable<string>? Erros { get; }

    public bool EhSucesso => Erros is null || !Erros.Any();

    public bool EhFalha => !EhSucesso;

    public static Resultado Sucesso()
    {
        return new Resultado();
    }

    public static Resultado Falha(IEnumerable<string> erros)
    {
        return new Resultado(erros);
    }

    public static Resultado Falha(string erro)
    {
        return new Resultado(new List<string> { erro });
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

    private Resultado(T? instancia = default, List<string>? erros = null) : base(erros)
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

    public static Resultado<T> Sucesso(T? instancia = default)
    {
        return new Resultado<T>(instancia);
    }

    public static new Resultado<T> Falha(string erro)
    {
        return new Resultado<T>(default, new List<string> { erro });
    }

    public static new Resultado<T> Falha(IEnumerable<string> erros)
    {
        return new Resultado<T>(default, erros.ToList());
    }

    public static Resultado<T> Tentar(Func<T> funcao)
    {
        try
        {
            return Sucesso(funcao());
        }
        catch (Exception excecao)
        {
            return Falha(excecao.Message);
        }
    }

    public static async Task<Resultado<T>> TentarAsync(Func<Task<Resultado<T>>> funcao)
    {
        try
        {
            return await funcao();
        }
        catch (Exception excecao)
        {
            return Falha(excecao.Message);
        }
    }

    public Resultado<object> ComFalha()
    {
        return Resultado<object>.Falha(Erros!);
    }

    public static Resultado<T[]> Combinar(params Resultado<T>[] resultados)
    {
        var falhas = resultados.Where(resultado => resultado.EhFalha).ToList();

        if (falhas.Count > 0)
        {
            var erros = falhas
                .SelectMany(resultado => resultado.Erros!)
                .ToList();

            return Resultado<T[]>.Falha(erros);
        }

        var instancias = resultados
            .Select(resultado => resultado.Instancia)
            .ToArray();

        return Resultado<T[]>.Sucesso(instancias);
    }

    public static async Task<Resultado<T[]>> CombinarAsync(params Task<Resultado<T>>[] resultados)
    {
        var resultadosResolvidos = await Task.WhenAll(resultados);

        return Combinar(resultadosResolvidos);
    }
}

public sealed record ResultadoPaginado<T>(
    IReadOnlyCollection<T> Itens,
    int NumeroPagina,
    int TamanhoPagina,
    int TotalRegistros)
{
    public int TotalPaginas =>
        TotalRegistros <= 0
            ? 0
            : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
}
