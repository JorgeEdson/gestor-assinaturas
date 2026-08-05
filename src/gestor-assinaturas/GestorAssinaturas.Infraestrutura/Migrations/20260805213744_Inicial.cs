using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAssinaturas.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Identificador = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "Faturas",
                columns: table => new
                {
                    Identificador = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificadorDaAssinatura = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Moeda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DataDeVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturas", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "Planos",
                columns: table => new
                {
                    Identificador = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrecoValor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoMoeda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CicloDeCobranca = table.Column<int>(type: "int", nullable: false),
                    PeriodoDeTrialEmDias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planos", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Identificador = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificadorDoCliente = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificadorPlano = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataDeInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataDeTerminoDoTrial = table.Column<DateOnly>(type: "date", nullable: true),
                    DataDeCancelamentoAgendado = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Planos_IdentificadorPlano",
                        column: x => x.IdentificadorPlano,
                        principalTable: "Planos",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_IdentificadorDoCliente",
                table: "Assinaturas",
                column: "IdentificadorDoCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_IdentificadorPlano",
                table: "Assinaturas",
                column: "IdentificadorPlano");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_IdentificadorDaAssinatura",
                table: "Faturas",
                column: "IdentificadorDaAssinatura");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Faturas");

            migrationBuilder.DropTable(
                name: "Planos");
        }
    }
}
