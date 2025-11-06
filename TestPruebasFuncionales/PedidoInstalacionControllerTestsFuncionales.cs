using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TestPruebasFuncionales
{
    [TestFixture]
    public class PedidoInstalacionControllerTestsFuncionales
    {
        private ApplicationDbContext _context;
        private PedidoInstalacionController _controller;
        private Mock<UserManager<IdentityUser>> _mockUserManager;

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _controller.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PedidoInstalacionTestDb")
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            _context = new ApplicationDbContext(options);

            // Sembrar datos requeridos
            var cargoTecnico = new Cargo { cargo_id = 1, titulo_cargo = "Jefe de Soporte Técnico" };
            var cargoAdmin = new Cargo { cargo_id = 2, titulo_cargo = "Administrador" };
            _context.Cargos.AddRange(cargoTecnico, cargoAdmin);

            var cliente1 = new Cliente
            {
                ClienteId = 1,
                Nombres = "Juan Pérez",
                Dni = "12345678",
                Direccion = "Calle 123",
                Correo = "juan@test.com",
                Telefono = "987654321",
                Password = "123456",
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };
            var cliente2 = new Cliente
            {
                ClienteId = 2,
                Nombres = "María García",
                Dni = "87654321",
                Direccion = "Calle 456",
                Correo = "maria@test.com",
                Telefono = "987654322",
                Password = "123456",
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };
            _context.Clientes.AddRange(cliente1, cliente2);

            var plan = new PlanServicio
            {
                PlanId = 1,
                NombrePlan = "Plan Básico",
                Descripcion = "Plan básico de internet",
                TipoServicio = "Internet",
                PrecioMensual = 120,
                Velocidad = 150,
                Estado = "Activo"
            };
            _context.PlanServicios.Add(plan);

            var empleadoTecnico = new Empleado
            {
                empleado_id = 1,
                nombres = "Carlos López",
                dni = "11111111",
                correo = "carlos@test.com",
                telefono = "999888777",
                cargo_id = 1,
                estado = "activo",
                fecha_inicio = DateTime.Now,
                password = "123456"
            };
            var empleadoAdmin = new Empleado
            {
                empleado_id = 2,
                nombres = "Ana Martínez",
                dni = "22222222",
                correo = "ana@test.com",
                telefono = "999888776",
                cargo_id = 2,
                estado = "activo",
                fecha_inicio = DateTime.Now,
                password = "123456"
            };
            _context.Empleados.AddRange(empleadoTecnico, empleadoAdmin);

            var contrato1 = new Contrato
            {
                Id = 1,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 2,
                FechaContrato = DateTime.Now,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "Activo"
            };
            var contrato2 = new Contrato
            {
                Id = 2,
                ClienteId = 2,
                PlanId = 1,
                EmpleadoId = 2,
                FechaContrato = DateTime.Now,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "Activo"
            };
            _context.Contratos.AddRange(contrato1, contrato2);

            var pedido1 = new PedidoInstalacion
            {
                PedidoId = 1,
                ContratoId = 1,
                EmpleadoId = 1,
                FechaInstalacion = DateTime.Now.AddDays(3),
                EstadoInstalacion = "Pendiente",
                Observaciones = "Instalación programada",
                JefeACargo = "Jefe de Soporte Técnico"
            };
            var pedido2 = new PedidoInstalacion
            {
                PedidoId = 2,
                ContratoId = 2,
                EmpleadoId = 1,
                FechaInstalacion = DateTime.Now.AddDays(5),
                EstadoInstalacion = "En proceso",
                Observaciones = "En progreso",
                JefeACargo = "Jefe de Soporte Técnico"
            };
            _context.PedidosInstalacion.AddRange(pedido1, pedido2);

            _context.SaveChanges();

            // UserManager mock (si lo necesitas para autenticación)
            var mockUserStore = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

            _controller = new PedidoInstalacionController(_context);
        }

        [Test]
        public async Task Index_DebeRetornarVistaConTodosLosPedidos()
        {
            // Simular usuario autenticado y TempData
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, "carlos@test.com"),
                new Claim(ClaimTypes.Role, "Jefe de Soporte Técnico")
            }, "mock"))
                }
            };

            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            // Act
            var result = await _controller.Index(null);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);

            var model = (result as ViewResult).Model as List<PedidoInstalacion>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public async Task Index_ConFiltroPendiente_DebeRetornarSoloPedidosPendientes()
        {
            // Simular usuario y TempData
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, "carlos@test.com"),
                new Claim(ClaimTypes.Role, "Jefe de Soporte Técnico")
            }, "mock"))
                }
            };

            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );

            // Act
            var result = await _controller.Index("Pendiente");

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var model = (result as ViewResult).Model as List<PedidoInstalacion>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("Pendiente", model[0].EstadoInstalacion);
        }


        [Test]
        public async Task Detalles_ConIdValido_DebeRetornarVistaConPedido()
        {
            var result = await _controller.Details(1);
            Assert.IsInstanceOf<ViewResult>(result);

            var model = (result as ViewResult).Model as PedidoInstalacion;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.PedidoId);
        }

        [Test]
        public async Task Detalles_ConIdInvalido_DebeRetornarNotFound()
        {
            var result = await _controller.Details(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public void Crear_Get_DebeRetornarVista()
        {
            var result = _controller.Create();
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Crear_Post_Valido_DebeCrearPedidoYRedirigir()
        {
            var nuevoPedido = new PedidoInstalacion
            {
                ContratoId = 1,
                EmpleadoId = 1,
                FechaInstalacion = DateTime.Now.AddDays(7),
                EstadoInstalacion = "Programado",
                Observaciones = "Nueva instalación"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "carlos@test.com"),
                new Claim(ClaimTypes.Role, "Jefe de Soporte Técnico")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            var result = await _controller.Create(nuevoPedido);
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Index", (result as RedirectToActionResult).ActionName);

            var pedidoCreado = await _context.PedidosInstalacion.FirstOrDefaultAsync(p => p.Observaciones == "Nueva instalación");
            Assert.IsNotNull(pedidoCreado);
            Assert.AreEqual("Jefe de Soporte Técnico", pedidoCreado.JefeACargo);
        }

        [Test]
        public async Task Editar_Post_Valido_DebeActualizarPedido()
        {
            var pedidoExistente = await _context.PedidosInstalacion.FindAsync(1);
            var pedidoActualizado = new PedidoInstalacion
            {
                PedidoId = 1,
                ContratoId = pedidoExistente.ContratoId,
                EmpleadoId = pedidoExistente.EmpleadoId,
                FechaInstalacion = DateTime.Now.AddDays(10),
                EstadoInstalacion = "Completado",
                Observaciones = "Pedido actualizado"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "carlos@test.com"),
                new Claim(ClaimTypes.Role, "Jefe de Soporte Técnico")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            var result = await _controller.Edit(pedidoActualizado);
            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var pedidoDb = await _context.PedidosInstalacion.FindAsync(1);
            Assert.AreEqual("Completado", pedidoDb.EstadoInstalacion);
            Assert.AreEqual("Pedido actualizado", pedidoDb.Observaciones);
        }

        [Test]
        public async Task Cancelar_ConIdValido_DebeCambiarEstado()
        {
            // Inicializar TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            // Simular usuario autenticado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "carlos@test.com"),
                new Claim(ClaimTypes.Role, "Jefe de Soporte Técnico")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            // Act
            var result = await _controller.Cancelar(1);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var pedidoCancelado = await _context.PedidosInstalacion.FindAsync(1);
            Assert.AreEqual("Cancelado", pedidoCancelado.EstadoInstalacion);
        }


        [Test]
        public async Task Cancelar_ConIdInvalido_DebeRetornarNotFound()
        {
            var result = await _controller.Cancelar(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}