using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
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

    public async Task<Resultado<Assinatura>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return await Resultado<Assinatura>.TentarAsync(async () =>
        {
            var assinatura = await _contextoDeDados.Assinaturas
                .FirstOrDefaultAsync(assinaturaArmazenada => assinaturaArmazenada.Identificador == identificador, cancellationToken);

            return Resultado<Assinatura>.Sucesso(assinatura);
        });
    }

    public async Task AdicionarAsync(Assinatura assinatura, CancellationToken cancellationToken = default)
    {
        await _contextoDeDados.Assinaturas.AddAsync(assinatura, cancellationToken);
    }
}
