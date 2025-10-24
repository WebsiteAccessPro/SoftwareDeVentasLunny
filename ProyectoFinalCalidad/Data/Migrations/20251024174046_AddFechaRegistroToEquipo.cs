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

            migrationBuilder.CreateTable(
                name: "Cargo",
                columns: table => new
                {
                    cargo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    titulo_cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.cargo_id);
                });

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    cliente_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dni = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    password = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.cliente_id);
                });

            migrationBuilder.CreateTable(
                name: "Equipo",
                columns: table => new
                {
                    equipo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_equipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    cantidad_stock = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipo", x => x.equipo_id);
                });

            migrationBuilder.CreateTable(
                name: "Zona_cobertura",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_zona = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    distrito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zona_cobertura", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Empleado",
                columns: table => new
                {
                    empleado_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cargo_id = table.Column<int>(type: "int", nullable: false),
                    nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dni = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    correo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_fin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleado", x => x.empleado_id);
                    table.ForeignKey(
                        name: "FK_Empleado_Cargo_cargo_id",
                        column: x => x.cargo_id,
                        principalTable: "Cargo",
                        principalColumn: "cargo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plan_servicio",
                columns: table => new
                {
                    plan_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_plan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    tipo_servicio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    velocidad = table.Column<int>(type: "int", nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    precio_mensual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    zona_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plan_servicio", x => x.plan_id);
                    table.ForeignKey(
                        name: "FK_Plan_servicio_Zona_cobertura_zona_id",
                        column: x => x.zona_id,
                        principalTable: "Zona_cobertura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contrato",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cliente_id = table.Column<int>(type: "int", nullable: false),
                    plan_id = table.Column<int>(type: "int", nullable: false),
                    empleado_id = table.Column<int>(type: "int", nullable: false),
                    fecha_contrato = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrato", x => x.id);
                    table.ForeignKey(
                        name: "FK_Contrato_Cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "Cliente",
                        principalColumn: "cliente_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contrato_Empleado_empleado_id",
                        column: x => x.empleado_id,
                        principalTable: "Empleado",
                        principalColumn: "empleado_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contrato_Plan_servicio_plan_id",
                        column: x => x.plan_id,
                        principalTable: "Plan_servicio",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContratoEquipo",
                columns: table => new
                {
                    contrato_equipo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contrato_id = table.Column<int>(type: "int", nullable: false),
                    equipo_id = table.Column<int>(type: "int", nullable: false),
                    fecha_asignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratoEquipo", x => x.contrato_equipo_id);
                    table.ForeignKey(
                        name: "FK_ContratoEquipo_Contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContratoEquipo_Equipo_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "Equipo",
                        principalColumn: "equipo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    pago_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contrato_id = table.Column<int>(type: "int", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    estado_pago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_de_vencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_pago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    metodo_pago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.pago_id);
                    table.ForeignKey(
                        name: "FK_Pago_Contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoInstalacion",
                columns: table => new
                {
                    pedido_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contrato_id = table.Column<int>(type: "int", nullable: false),
                    empleado_id = table.Column<int>(type: "int", nullable: false),
                    fecha_instalacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado_instalacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    observaciones = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoInstalacion", x => x.pedido_id);
                    table.ForeignKey(
                        name: "FK_PedidoInstalacion_Contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "Contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoInstalacion_Empleado_empleado_id",
                        column: x => x.empleado_id,
                        principalTable: "Empleado",
                        principalColumn: "empleado_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
            migrationBuilder.DropTable(
                name: "ContratoEquipo");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "PedidoInstalacion");

            migrationBuilder.DropTable(
                name: "Equipo");

            migrationBuilder.DropTable(
                name: "Contrato");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropTable(
                name: "Empleado");

            migrationBuilder.DropTable(
                name: "Plan_servicio");

            migrationBuilder.DropTable(
                name: "Cargo");

            migrationBuilder.DropTable(
                name: "Zona_cobertura");

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
