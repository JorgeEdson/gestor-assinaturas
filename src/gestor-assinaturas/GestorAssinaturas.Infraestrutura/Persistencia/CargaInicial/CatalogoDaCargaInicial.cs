using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Infraestrutura.Persistencia.CargaInicial;

public sealed record PlanoDaCargaInicial(
    Guid Identificador,
    string Nome,
    decimal Valor,
    string Moeda,
    TipoDeCicloDeCobranca CicloDeCobranca,
    int PeriodoDeTrialEmDias);

public sealed record ClienteDaCargaInicial(
    Guid Identificador,
    string Nome,
    string Email);

public static class CatalogoDaCargaInicial
{
    public static readonly IReadOnlyList<PlanoDaCargaInicial> Planos =
    [
        new PlanoDaCargaInicial(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Plano Profissional",
            99.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 14),
        new PlanoDaCargaInicial(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Plano Essencial",
            49.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0),
        new PlanoDaCargaInicial(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Plano Cobranca Recusada",
            49.99m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0),
        new PlanoDaCargaInicial(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Plano Corporativo Anual",
            999.00m,
            "BRL",
            TipoDeCicloDeCobranca.Anual,
            PeriodoDeTrialEmDias: 30)
    ];

    public static readonly IReadOnlyList<ClienteDaCargaInicial> Clientes =
    [
        new ClienteDaCargaInicial(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Empresa Alfa",
            "contato@empresaalfa.com"),
        new ClienteDaCargaInicial(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Empresa Beta",
            "financeiro@empresabeta.com")
    ];
}
