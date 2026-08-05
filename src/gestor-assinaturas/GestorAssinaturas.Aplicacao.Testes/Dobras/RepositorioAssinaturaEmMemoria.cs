using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioAssinaturaEmMemoria : IRepositorioAssinatura
{
    private readonly Dictionary<Guid, Assinatura> _assinaturas = new();

    public IReadOnlyCollection<Assinatura> Assinaturas => _assinaturas.Values.ToList();

    public void Semear(Assinatura assinatura)
    {
        _assinaturas[assinatura.Identificador] = assinatura;
    }

    public Task<Resultado<Assinatura>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return _assinaturas.TryGetValue(identificador, out var assinatura)
            ? Task.FromResult(Resultado<Assinatura>.Sucesso(assinatura))
            : Task.FromResult(Resultado<Assinatura>.Sucesso());
    }

    public Task AdicionarAsync(Assinatura assinatura, CancellationToken cancellationToken = default)
    {
        _assinaturas[assinatura.Identificador] = assinatura;

        return Task.CompletedTask;
    }
}
