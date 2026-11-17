using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioAssinaturaEmMemoria : IRepositorioAssinatura
{
    private readonly Dictionary<Guid, Assinatura> _assinaturas = new();

    public IReadOnlyCollection<Assinatura> Assinaturas => _assinaturas.Values.ToList();

    public void Semear(Assinatura assinatura)
    {
        _assinaturas[assinatura.Identificador] = assinatura;
    }

    public Task<Assinatura?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        _assinaturas.TryGetValue(identificador, out var assinatura);

        return Task.FromResult(assinatura);
    }

    public Task AdicionarAsync(Assinatura assinatura, CancellationToken tokenDeCancelamento = default)
    {
        _assinaturas[assinatura.Identificador] = assinatura;

        return Task.CompletedTask;
    }
}
