using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class RepositorioFatura : IRepositorioFatura
{
    private readonly ContextoDeDados _contextoDeDados;

    public RepositorioFatura(ContextoDeDados contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Fatura?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        return await _contextoDeDados.Faturas
            .FirstOrDefaultAsync(faturaArmazenada => faturaArmazenada.Identificador == identificador, tokenDeCancelamento);
    }

    public async Task<IReadOnlyCollection<Fatura>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken tokenDeCancelamento = default)
    {
        return await _contextoDeDados.Faturas
            .Where(faturaArmazenada => faturaArmazenada.IdentificadorDaAssinatura == identificadorDaAssinatura)
            .ToListAsync(tokenDeCancelamento);
    }

    public async Task AdicionarAsync(Fatura fatura, CancellationToken tokenDeCancelamento = default)
    {
        await _contextoDeDados.Faturas.AddAsync(fatura, tokenDeCancelamento);
    }
}
