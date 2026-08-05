using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.EntityFrameworkCore.Storage;

namespace GestorAssinaturas.Infraestrutura.Persistencia;

public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly GestorAssinaturasDbContext _contextoDeDados;
    private IDbContextTransaction? _transacaoAtual;

    public UnitOfWork(
        GestorAssinaturasDbContext contextoDeDados,
        IRepositorioPlano planos,
        IRepositorioCliente clientes,
        IRepositorioAssinatura assinaturas,
        IRepositorioFatura faturas)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);
        ArgumentNullException.ThrowIfNull(planos);
        ArgumentNullException.ThrowIfNull(clientes);
        ArgumentNullException.ThrowIfNull(assinaturas);
        ArgumentNullException.ThrowIfNull(faturas);

        _contextoDeDados = contextoDeDados;
        Planos = planos;
        Clientes = clientes;
        Assinaturas = assinaturas;
        Faturas = faturas;
    }

    public IRepositorioPlano Planos { get; }

    public IRepositorioCliente Clientes { get; }

    public IRepositorioAssinatura Assinaturas { get; }

    public IRepositorioFatura Faturas { get; }

    public async Task<Resultado<int>> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return await Resultado<int>.TentarAsync(async () =>
        {
            var quantidadeDeRegistrosAfetados = await _contextoDeDados.SaveChangesAsync(cancellationToken);

            return Resultado<int>.Sucesso(quantidadeDeRegistrosAfetados);
        });
    }

    public async Task<Resultado<bool>> IniciarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return await Resultado<bool>.TentarAsync(async () =>
        {
            if (_transacaoAtual is not null)
            {
                return Resultado<bool>.Falha("Já existe uma transação em andamento.");
            }

            _transacaoAtual = await _contextoDeDados.Database.BeginTransactionAsync(cancellationToken);

            return Resultado<bool>.Sucesso(true);
        });
    }

    public async Task<Resultado<bool>> ConfirmarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return await Resultado<bool>.TentarAsync(async () =>
        {
            if (_transacaoAtual is null)
            {
                return Resultado<bool>.Falha("Não existe uma transação em andamento para confirmar.");
            }

            await _transacaoAtual.CommitAsync(cancellationToken);
            await _transacaoAtual.DisposeAsync();
            _transacaoAtual = null;

            return Resultado<bool>.Sucesso(true);
        });
    }

    public async Task<Resultado<bool>> DesfazerTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return await Resultado<bool>.TentarAsync(async () =>
        {
            if (_transacaoAtual is null)
            {
                return Resultado<bool>.Falha("Não existe uma transação em andamento para desfazer.");
            }

            await _transacaoAtual.RollbackAsync(cancellationToken);
            await _transacaoAtual.DisposeAsync();
            _transacaoAtual = null;

            return Resultado<bool>.Sucesso(true);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_transacaoAtual is not null)
        {
            await _transacaoAtual.DisposeAsync();
            _transacaoAtual = null;
        }
    }
}
