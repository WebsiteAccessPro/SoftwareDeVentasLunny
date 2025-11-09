using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoFinalCalidad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipoUnidadYRelacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "equipo_unidad_id",
                table: "ContratoEquipo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipoUnidad",
                columns: table => new
                {
                    equipo_unidad_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    equipo_id = table.Column<int>(type: "int", nullable: false),
                    codigo_unidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    estado_unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipoUnidad", x => x.equipo_unidad_id);
                    table.ForeignKey(
                        name: "FK_EquipoUnidad_Equipo_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "Equipo",
                        principalColumn: "equipo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContratoEquipo_equipo_unidad_id",
                table: "ContratoEquipo",
                column: "equipo_unidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_EquipoUnidad_equipo_id",
                table: "EquipoUnidad",
                column: "equipo_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratoEquipo_EquipoUnidad_equipo_unidad_id",
                table: "ContratoEquipo",
                column: "equipo_unidad_id",
                principalTable: "EquipoUnidad",
                principalColumn: "equipo_unidad_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContratoEquipo_EquipoUnidad_equipo_unidad_id",
                table: "ContratoEquipo");

            migrationBuilder.DropTable(
                name: "EquipoUnidad");

            migrationBuilder.DropIndex(
                name: "IX_ContratoEquipo_equipo_unidad_id",
                table: "ContratoEquipo");

            migrationBuilder.DropColumn(
                name: "equipo_unidad_id",
                table: "ContratoEquipo");
        }
    }
}
