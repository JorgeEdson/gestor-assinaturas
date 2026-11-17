using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioFaturaEmMemoria : IRepositorioFatura
{
    private readonly Dictionary<Guid, Fatura> _faturas = new();

    public IReadOnlyCollection<Fatura> Faturas => _faturas.Values.ToList();

    public void Semear(Fatura fatura)
    {
        _faturas[fatura.Identificador] = fatura;
    }

    public Task<Fatura?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        _faturas.TryGetValue(identificador, out var fatura);

        return Task.FromResult(fatura);
    }

    public Task<IReadOnlyCollection<Fatura>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken tokenDeCancelamento = default)
    {
        IReadOnlyCollection<Fatura> faturasDaAssinatura = _faturas.Values
            .Where(fatura => fatura.IdentificadorDaAssinatura == identificadorDaAssinatura)
            .ToList();

        return Task.FromResult(faturasDaAssinatura);
    }

    public Task AdicionarAsync(Fatura fatura, CancellationToken tokenDeCancelamento = default)
    {
        _faturas[fatura.Identificador] = fatura;

        return Task.CompletedTask;
    }
}
