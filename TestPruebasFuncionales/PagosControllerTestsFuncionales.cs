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
using ProyectoFinalCalidad.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TestPruebasFuncionales
{
    [TestFixture]
    public class PagosControllerTestsFuncionales
    {
        private ApplicationDbContext _context;
        private PagosController _controller;

        [SetUp]
        public void Setup()
        {
            // Configurar base de datos en memoria
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new PagosController(_context);

            // Inicializar TempData y ControllerContext
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            // Simular usuario con rol Administrador
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Usuario")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = userClaims }
            };

            // Sembrar datos de prueba
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Zona de cobertura
            var zona = new ZonaCobertura
            {
                Id = 1,
                NombreZona = "Zona Test",
                Distrito = "Distrito Test",
                Descripcion = "Zona de prueba"
            };
            _context.ZonaCoberturas.Add(zona);

            // Plan de servicio
            var planServicio = new PlanServicio
            {
                PlanId = 1,
                NombrePlan = "Plan Premium",
                PrecioMensual = 100.00m,
                Velocidad = 200,
                TipoServicio = "Internet",
                ZonaId = zona.Id,
                Descripcion = "Plan premium de internet"
            };
            _context.PlanServicios.Add(planServicio);

            // Cliente
            var cliente = new Cliente
            {
                ClienteId = 1,
                Nombres = "María García",
                Dni = "87654321",
                Correo = "maria@test.com",
                Direccion = "Calle Test 456",
                Telefono = "987654322",
                Password = "password123",
                Estado = "activo",
                FechaRegistro = DateTime.Now
            };
            _context.Clientes.Add(cliente);

            // Contrato
            var contrato = new Contrato
            {
                Id = 1,
                ClienteId = cliente.ClienteId,
                PlanId = planServicio.PlanId,
                EmpleadoId = 1,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12),
                Estado = "activo"
            };
            _context.Contratos.Add(contrato);

            // Pago inicial
            var pago = new Pago
            {
                PagoId = 1,
                ContratoId = contrato.Id,
                Monto = planServicio.PrecioMensual,
                EstadoPago = "pendiente",
                FechaDeVencimiento = DateTime.Now.AddDays(15),
                MetodoPago = "Tarjeta de crédito"
            };
            _context.Pagos.Add(pago);

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _controller?.Dispose();
        }

        [Test]
        public async Task BuscarPorDni_ClienteExistenteConContrato_RetornaVistaConDatos()
        {
            var dni = "87654321";

            var result = await _controller.BuscarPorDni(dni, false);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("MostrarDatosPago", redirectResult.ActionName);
            Assert.IsTrue(redirectResult.RouteValues.ContainsKey("dni"));
            Assert.AreEqual(dni, redirectResult.RouteValues["dni"]);
        }

        [Test]
        public async Task BuscarPorDni_ClienteNoExistente_RetornaVistaConMensajeError()
        {
            var dni = "99999999";
            var result = await _controller.BuscarPorDni(dni, false);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.ViewData["Mensaje"]);
            StringAssert.Contains("No se encontró ningún cliente", viewResult.ViewData["Mensaje"].ToString());
        }

        [Test]
        public async Task BuscarPorDni_ClienteInactivo_RetornaVistaConMensajeError()
        {
            var cliente = _context.Clientes.First();
            cliente.Estado = "inactivo";
            await _context.SaveChangesAsync();

            var dni = cliente.Dni;
            var result = await _controller.BuscarPorDni(dni, false);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.ViewData["Mensaje"]);
            StringAssert.Contains("inhabilitado", viewResult.ViewData["Mensaje"].ToString());
        }

        [Test]
        public async Task MostrarDatosPago_ConDatosValidos_RetornaVistaConViewModel()
        {
            var dni = "87654321";
            var result = await _controller.MostrarDatosPago(dni);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsInstanceOf<PagoViewModel>(viewResult.Model);

            var model = viewResult.Model as PagoViewModel;
            Assert.IsNotNull(model.Cliente);
            Assert.IsNotNull(model.Contrato);
            Assert.IsNotNull(model.Pagos);
            Assert.Greater(model.Pagos.Count, 0);
        }

        [Test]
        public async Task MostrarDatosPago_ClienteSinContrato_RetornaNotFound()
        {
            var clienteSinContrato = new Cliente
            {
                ClienteId = 2,
                Nombres = "Cliente Sin Contrato",
                Dni = "11111111",
                Correo = "sincontrato@test.com",
                Direccion = "Calle Sin Contrato 123",
                Telefono = "999888777",
                Password = "password123",
                Estado = "activo",
                FechaRegistro = DateTime.Now
            };

            _context.Clientes.Add(clienteSinContrato);
            await _context.SaveChangesAsync();

            var result = await _controller.MostrarDatosPago("11111111");
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public void Checkout_PagoExistente_RetornaVistaConPago()
        {
            var result = _controller.Checkout(1);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsInstanceOf<Pago>(viewResult.Model);

            var model = viewResult.Model as Pago;
            Assert.AreEqual(1, model.PagoId);
            Assert.AreEqual(100.00m, model.Monto);
        }

        [Test]
        public void Checkout_PagoNoExistente_RetornaNotFound()
        {
            var result = _controller.Checkout(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task BuscarPorDni_Post_ConDniValido_RedireccionaCorrectamente()
        {
            var dni = "87654321";
            var result = _controller.BuscarPorDni(dni);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("BuscarPorDni", redirectResult.ActionName);
            Assert.AreEqual(dni, redirectResult.RouteValues["dni"]);
        }

        [Test]
        public async Task BuscarPorDni_Post_ConDniVacio_MuestraMensajeError()
        {
            var result = _controller.BuscarPorDni("");

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult.ViewData["Mensaje"]);
            StringAssert.Contains("ingrese un DNI válido", viewResult.ViewData["Mensaje"].ToString());
        }
    }
}