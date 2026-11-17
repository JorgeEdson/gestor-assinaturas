using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class RepositorioPlano : IRepositorioPlano
{
    private readonly ContextoDeDados _contextoDeDados;

    public RepositorioPlano(ContextoDeDados contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Plano?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        return await _contextoDeDados.Planos
            .FirstOrDefaultAsync(planoArmazenado => planoArmazenado.Identificador == identificador, tokenDeCancelamento);
    }

    public async Task AdicionarAsync(Plano plano, CancellationToken tokenDeCancelamento = default)
    {
        await _contextoDeDados.Planos.AddAsync(plano, tokenDeCancelamento);
    }
}
