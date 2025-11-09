using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoFinalCalidad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaRegistroToEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_Contrato_cliente_id",
                table: "Contrato",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_Contrato_empleado_id",
                table: "Contrato",
                column: "empleado_id");

            migrationBuilder.CreateIndex(
                name: "IX_Contrato_plan_id",
                table: "Contrato",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoEquipo_contrato_id",
                table: "ContratoEquipo",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContratoEquipo_equipo_id",
                table: "ContratoEquipo",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_cargo_id",
                table: "Empleado",
                column: "cargo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_contrato_id",
                table: "Pago",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInstalacion_contrato_id",
                table: "PedidoInstalacion",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInstalacion_empleado_id",
                table: "PedidoInstalacion",
                column: "empleado_id");

            migrationBuilder.CreateIndex(
                name: "IX_Plan_servicio_zona_id",
                table: "Plan_servicio",
                column: "zona_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
