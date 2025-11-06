using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Repositories;
using ProyectoFinalCalidad.Services;
using System.Security.Claims;

namespace TestPruebasFuncionales
{
    public class ContratoControllerTestsFuncionales
    {
        private ApplicationDbContext _context;
        private ContratoController _controller;

        [SetUp]
        public void Setup()
        {
            // Base de datos en memoria
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            _context = new ApplicationDbContext(options);

            // Servicio de contrato
            IContratoService contratoService = new ContratoRepository(_context);

            _controller = new ContratoController(contratoService, _context);

            // Simular usuario logueado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Usuario")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Inicializar TempData para evitar NullReferenceException
            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>());

            // Sembrar datos iniciales
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Cargo necesario para Empleado
            var cargo = new Cargo { cargo_id = 1, titulo_cargo = "Técnico de Instalación" };

            // Empleado
            var empleado = new Empleado
            {
                empleado_id = 1,
                nombres = "Carlos López",
                dni = "87654321",
                correo = "carlos@test.com",
                telefono = "123456789",
                password = "password123",
                estado = "activo",
                cargo_id = 1,
                fecha_inicio = DateTime.Now
            };

            // Cliente
            var cliente = new Cliente
            {
                ClienteId = 1,
                Nombres = "Juan Pérez",
                Dni = "12345678",
                Correo = "juan@test.com",
                Direccion = "Av. Test 123",
                Telefono = "987654321",
                Password = "password123",
                Estado = "activo",
                FechaRegistro = DateTime.Now
            };

            // ZonaCobertura con todos los campos obligatorios
            var zona = new ZonaCobertura
            {
                Id = 1,
                NombreZona = "Zona Test",
                Distrito = "Distrito Test",
                Descripcion = "Zona de prueba"
            };

            // PlanServicio (todos los campos obligatorios)
            var plan = new PlanServicio
            {
                PlanId = 1,
                NombrePlan = "Plan Básico",
                TipoServicio = "Internet",
                Velocidad = 50,
                Descripcion = "Plan básico de prueba",
                PrecioMensual = 59.90m,
                Estado = "activo",
                ZonaId = zona.Id
            };

            _context.Cargos.Add(cargo);
            _context.Empleados.Add(empleado);
            _context.Clientes.Add(cliente);
            _context.ZonaCoberturas.Add(zona);
            _context.PlanServicios.Add(plan);
            _context.SaveChanges();
        }

        [Test]
        public async Task AgregarContrato_FlujoCompleto_RetornaRedirectToAction()
        {
            var contrato = new Contrato
            {
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                Estado = "activo"
            };

            var result = await _controller.AgregarContrato(contrato, 12);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("MostrarContratos", redirectResult.ActionName);

            var contratoCreado = await _context.Contratos.FirstOrDefaultAsync(c => c.ClienteId == 1);
            Assert.IsNotNull(contratoCreado);
            Assert.AreEqual(DateTime.Now.AddMonths(12).Date, contratoCreado.FechaFin.Date);
        }

        [Test]
        public async Task MostrarContratos_ConContratosExistentes_RetornaVistaConLista()
        {
            var contrato = new Contrato
            {
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "activo"
            };
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            var result = await _controller.MostrarContratos();

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as System.Collections.Generic.List<Contrato>;
            Assert.IsNotNull(model);
            Assert.Greater(model.Count, 0);
        }

        [Test]
        public async Task EditarContrato_FlujoCompleto_ActualizaContratoCorrectamente()
        {
            var contrato = new Contrato
            {
                Id = 1,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "activo"
            };
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            var contratoEditado = new Contrato
            {
                Id = 1,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now.AddDays(30),
                Estado = "activo"
            };

            var result = await _controller.EditarContrato(1, contratoEditado, 24);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("MostrarContratos", redirectResult.ActionName);

            var contratoActualizado = await _context.Contratos.FindAsync(1);
            Assert.IsNotNull(contratoActualizado);
            Assert.AreEqual(DateTime.Now.AddDays(30).AddMonths(24).Date, contratoActualizado.FechaFin.Date);
        }

        [Test]
        public async Task DeshabilitarContrato_CambiaEstadoAInactivo()
        {
            var contrato = new Contrato
            {
                Id = 1,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "activo"
            };
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            var result = await _controller.Deshabilitar(1);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var contratoDeshabilitado = await _context.Contratos.FindAsync(1);
            Assert.AreEqual("inactivo", contratoDeshabilitado.Estado);
        }

        [Test]
        public async Task AgregarContrato_ModeloInvalido_RetornaVistaConErrores()
        {
            var contrato = new Contrato
            {
                ClienteId = 0,
                PlanId = 0,
                EmpleadoId = 0,
                FechaInicio = default,
                Estado = "activo"
            };

            _controller.ModelState.AddModelError("ClienteId", "El cliente es obligatorio");
            _controller.ModelState.AddModelError("PlanId", "El plan es obligatorio");

            var result = await _controller.AgregarContrato(contrato, 12);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.Model);
            Assert.IsFalse(_controller.ModelState.IsValid);
        }

        [Test]
        public async Task EditarContrato_ContratoNoExistente_RetornaNotFound()
        {
            var contrato = new Contrato
            {
                Id = 999,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                Estado = "activo"
            };

            var result = await _controller.EditarContrato(999, contrato, 12);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _controller?.Dispose();
        }
    }
}