using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioPlanoEmMemoria : IRepositorioPlano
{
    private readonly Dictionary<Guid, Plano> _planos = new();

    public IReadOnlyCollection<Plano> Planos => _planos.Values.ToList();

    public Task<Resultado<Plano>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return _planos.TryGetValue(identificador, out var plano)
            ? Task.FromResult(Resultado<Plano>.Sucesso(plano))
            : Task.FromResult(Resultado<Plano>.Sucesso());
    }

    public Task AdicionarAsync(Plano plano, CancellationToken cancellationToken = default)
    {
        _planos[plano.Identificador] = plano;

        return Task.CompletedTask;
    }
}
