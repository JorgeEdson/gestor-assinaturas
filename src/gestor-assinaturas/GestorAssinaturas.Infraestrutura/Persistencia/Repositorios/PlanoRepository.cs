using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class PlanoRepository : IRepositorioPlano
{
    private readonly GestorAssinaturasDbContext _contextoDeDados;

    public PlanoRepository(GestorAssinaturasDbContext contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Resultado<Plano>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return await Resultado<Plano>.TentarAsync(async () =>
        {
            var plano = await _contextoDeDados.Planos
                .FirstOrDefaultAsync(planoArmazenado => planoArmazenado.Identificador == identificador, cancellationToken);

            return Resultado<Plano>.Sucesso(plano);
        });
    }

    public async Task AdicionarAsync(Plano plano, CancellationToken cancellationToken = default)
    {
        await _contextoDeDados.Planos.AddAsync(plano, cancellationToken);
    }
}
