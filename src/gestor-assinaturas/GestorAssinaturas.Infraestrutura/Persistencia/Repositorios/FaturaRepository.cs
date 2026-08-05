using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class FaturaRepository : IRepositorioFatura
{
    private readonly GestorAssinaturasDbContext _contextoDeDados;

    public FaturaRepository(GestorAssinaturasDbContext contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Resultado<Fatura>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return await Resultado<Fatura>.TentarAsync(async () =>
        {
            var fatura = await _contextoDeDados.Faturas
                .FirstOrDefaultAsync(faturaArmazenada => faturaArmazenada.Identificador == identificador, cancellationToken);

            return Resultado<Fatura>.Sucesso(fatura);
        });
    }

    public async Task<Resultado<IReadOnlyCollection<Fatura>>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken cancellationToken = default)
    {
        return await Resultado<IReadOnlyCollection<Fatura>>.TentarAsync(async () =>
        {
            IReadOnlyCollection<Fatura> faturas = await _contextoDeDados.Faturas
                .Where(faturaArmazenada => faturaArmazenada.IdentificadorDaAssinatura == identificadorDaAssinatura)
                .ToListAsync(cancellationToken);

            return Resultado<IReadOnlyCollection<Fatura>>.Sucesso(faturas);
        });
    }

    public async Task AdicionarAsync(Fatura fatura, CancellationToken cancellationToken = default)
    {
        await _contextoDeDados.Faturas.AddAsync(fatura, cancellationToken);
    }
}
