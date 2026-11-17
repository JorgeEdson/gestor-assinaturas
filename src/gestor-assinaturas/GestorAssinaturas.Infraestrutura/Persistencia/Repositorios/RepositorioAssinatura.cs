using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class RepositorioAssinatura : IRepositorioAssinatura
{
    private readonly ContextoDeDados _contextoDeDados;

    public RepositorioAssinatura(ContextoDeDados contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Assinatura?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        return await _contextoDeDados.Assinaturas
            .FirstOrDefaultAsync(assinaturaArmazenada => assinaturaArmazenada.Identificador == identificador, tokenDeCancelamento);
    }

    public async Task AdicionarAsync(Assinatura assinatura, CancellationToken tokenDeCancelamento = default)
    {
        await _contextoDeDados.Assinaturas.AddAsync(assinatura, tokenDeCancelamento);
    }
}
