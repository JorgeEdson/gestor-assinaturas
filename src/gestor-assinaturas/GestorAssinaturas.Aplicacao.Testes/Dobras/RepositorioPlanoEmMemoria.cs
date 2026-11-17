using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioPlanoEmMemoria : IRepositorioPlano
{
    private readonly Dictionary<Guid, Plano> _planos = new();

    public IReadOnlyCollection<Plano> Planos => _planos.Values.ToList();

    public Task<Plano?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        _planos.TryGetValue(identificador, out var plano);

        return Task.FromResult(plano);
    }

    public Task AdicionarAsync(Plano plano, CancellationToken tokenDeCancelamento = default)
    {
        _planos[plano.Identificador] = plano;

        return Task.CompletedTask;
    }
}
