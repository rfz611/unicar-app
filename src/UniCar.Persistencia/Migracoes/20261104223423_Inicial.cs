using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UniCar.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Regras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Universidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Sigla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DominioEmail = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Curso = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Periodo = table.Column<int>(type: "INTEGER", nullable: false),
                    UniversidadeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Universidades_UniversidadeId",
                        column: x => x.UniversidadeId,
                        principalTable: "Universidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Preferencias",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemaEscuro = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvisosDeCarona = table.Column<bool>(type: "INTEGER", nullable: false),
                    MostrarCurso = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferencias", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Preferencias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Placa = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Cor = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MotoristaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veiculos_Usuarios_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Caronas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MotoristaId = table.Column<int>(type: "INTEGER", nullable: false),
                    VeiculoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Origem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Destino = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SaidaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Vagas = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoPorVaga = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caronas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caronas_Usuarios_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caronas_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaronaRegraCarona",
                columns: table => new
                {
                    CaronasId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegrasId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaronaRegraCarona", x => new { x.CaronasId, x.RegrasId });
                    table.ForeignKey(
                        name: "FK_CaronaRegraCarona_Caronas_CaronasId",
                        column: x => x.CaronasId,
                        principalTable: "Caronas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaronaRegraCarona_Regras_RegrasId",
                        column: x => x.RegrasId,
                        principalTable: "Regras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaronaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PassageiroId = table.Column<int>(type: "INTEGER", nullable: false),
                    Preco = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Caronas_CaronaId",
                        column: x => x.CaronaId,
                        principalTable: "Caronas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_PassageiroId",
                        column: x => x.PassageiroId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Regras",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Não fumante" },
                    { 2, "Música ambiente" },
                    { 3, "Aceita pet" }
                });

            migrationBuilder.InsertData(
                table: "Universidades",
                columns: new[] { "Id", "DominioEmail", "Nome", "Sigla" },
                values: new object[,]
                {
                    { 1, "ufpr.br", "Universidade Federal do Paraná", "UFPR" },
                    { 2, "pucpr.edu.br", "Pontifícia Universidade Católica do Paraná", "PUCPR" },
                    { 3, "utfpr.edu.br", "Universidade Tecnológica Federal do Paraná", "UTFPR" },
                    { 4, "up.edu.br", "Universidade Positivo", "Positivo" },
                    { 5, "fae.edu.br", "FAE Centro Universitário", "FAE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaronaRegraCarona_RegrasId",
                table: "CaronaRegraCarona",
                column: "RegrasId");

            migrationBuilder.CreateIndex(
                name: "IX_Caronas_MotoristaId",
                table: "Caronas",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Caronas_VeiculoId",
                table: "Caronas",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CaronaId_PassageiroId",
                table: "Reservas",
                columns: new[] { "CaronaId", "PassageiroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_PassageiroId",
                table: "Reservas",
                column: "PassageiroId");

            migrationBuilder.CreateIndex(
                name: "IX_Universidades_Sigla",
                table: "Universidades",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UniversidadeId",
                table: "Usuarios",
                column: "UniversidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_MotoristaId",
                table: "Veiculos",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_Placa",
                table: "Veiculos",
                column: "Placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaronaRegraCarona");

            migrationBuilder.DropTable(
                name: "Preferencias");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Regras");

            migrationBuilder.DropTable(
                name: "Caronas");

            migrationBuilder.DropTable(
                name: "Veiculos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Universidades");
        }
    }
}
