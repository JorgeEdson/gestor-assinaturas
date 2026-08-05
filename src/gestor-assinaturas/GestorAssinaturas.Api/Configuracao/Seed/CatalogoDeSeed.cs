using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Api.Configuracao.Seed;

public sealed record PlanoDeSeed(
    Guid Identificador,
    string Nome,
    decimal Valor,
    string Moeda,
    TipoDeCicloDeCobranca CicloDeCobranca,
    int PeriodoDeTrialEmDias);

public sealed record ClienteDeSeed(
    Guid Identificador,
    string Nome,
    string Email);

public static class CatalogoDeSeed
{
    public static readonly IReadOnlyList<PlanoDeSeed> Planos =
    [
        new PlanoDeSeed(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Plano Profissional",
            99.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 14),
        new PlanoDeSeed(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Plano Essencial",
            49.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0),
        new PlanoDeSeed(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Plano Cobranca Recusada",
            49.99m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0),
        new PlanoDeSeed(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Plano Corporativo Anual",
            999.00m,
            "BRL",
            TipoDeCicloDeCobranca.Anual,
            PeriodoDeTrialEmDias: 30)
    ];

    public static readonly IReadOnlyList<ClienteDeSeed> Clientes =
    [
        new ClienteDeSeed(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Empresa Alfa",
            "contato@empresaalfa.com"),
        new ClienteDeSeed(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Empresa Beta",
            "financeiro@empresabeta.com")
    ];
}
