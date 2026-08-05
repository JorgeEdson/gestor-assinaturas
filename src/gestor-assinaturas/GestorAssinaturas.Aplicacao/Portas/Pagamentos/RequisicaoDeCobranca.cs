using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Aplicacao.Portas.Pagamentos;

public sealed record RequisicaoDeCobranca(
    Guid IdentificadorDaFatura,
    Guid IdentificadorDaAssinatura,
    Guid IdentificadorDoCliente,
    Dinheiro Valor);
