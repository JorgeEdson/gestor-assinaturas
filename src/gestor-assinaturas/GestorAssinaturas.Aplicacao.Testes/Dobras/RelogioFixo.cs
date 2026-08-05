using GestorAssinaturas.Aplicacao.Portas;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RelogioFixo : IRelogioDoSistema
{
    private readonly DateOnly _dataAtual;

    public RelogioFixo(DateOnly dataAtual)
    {
        _dataAtual = dataAtual;
    }

    public DateOnly ObterDataAtual()
    {
        return _dataAtual;
    }

    public DateTimeOffset ObterInstanteAtual()
    {
        return new DateTimeOffset(_dataAtual.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
