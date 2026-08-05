using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioFaturaEmMemoria : IRepositorioFatura
{
    private readonly Dictionary<Guid, Fatura> _faturas = new();

    public IReadOnlyCollection<Fatura> Faturas => _faturas.Values.ToList();

    public void Semear(Fatura fatura)
    {
        _faturas[fatura.Identificador] = fatura;
    }

    public Task<Resultado<Fatura>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return _faturas.TryGetValue(identificador, out var fatura)
            ? Task.FromResult(Resultado<Fatura>.Sucesso(fatura))
            : Task.FromResult(Resultado<Fatura>.Sucesso());
    }

    public Task<Resultado<IReadOnlyCollection<Fatura>>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Fatura> faturasDaAssinatura = _faturas.Values
            .Where(fatura => fatura.IdentificadorDaAssinatura == identificadorDaAssinatura)
            .ToList();

        return Task.FromResult(Resultado<IReadOnlyCollection<Fatura>>.Sucesso(faturasDaAssinatura));
    }

    public Task AdicionarAsync(Fatura fatura, CancellationToken cancellationToken = default)
    {
        _faturas[fatura.Identificador] = fatura;

        return Task.CompletedTask;
    }
}
