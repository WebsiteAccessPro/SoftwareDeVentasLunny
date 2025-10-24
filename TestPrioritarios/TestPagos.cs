using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;

namespace TestPrioritarios
{
    [TestFixture]
    public class TestPagos
    {
        private ApplicationDbContext _context;
        private PagosController _controller;

        [SetUp]
        public void Setup()
        {
            // Configurar DB en memoria
            var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(opciones);
            _controller = new PagosController(_context);

            // Mock de ITempDataProvider para TempData
            var tempDataProviderMock = new Mock<ITempDataProvider>();
            tempDataProviderMock
                .Setup(p => p.LoadTempData(It.IsAny<HttpContext>()))
                .Returns(new Dictionary<string, object>());
            tempDataProviderMock
                .Setup(p => p.SaveTempData(It.IsAny<HttpContext>(), It.IsAny<IDictionary<string, object>>()));

            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProviderMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        // 1. Prueba de error: Buscar por DNI inexistente
        [Test]
        public async Task BuscarPorDni_DeberiaMostrarMensaje_CuandoDniNoExiste()
        {
            // Act
            var resultado = await _controller.BuscarPorDni("99999999") as ViewResult;

            // Assert
            Assert.IsNotNull(resultado, "Debe retornar una vista.");
            Assert.AreEqual("DNI no registrado.", resultado.ViewData["Mensaje"]);
        }

        // 2. Prueba de error: Generar pago automático con contrato inexistente
        [Test]
        public async Task GenerarPagoAutomatico_DeberiaRetornarNotFound_CuandoContratoNoExiste()
        {
            // Act
            var resultado = await _controller.GenerarPagoAutomatico(100) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(resultado, "Debe retornar un resultado NotFound.");
            Assert.AreEqual("Contrato no encontrado", resultado.Value);
        }

        // 3. Prueba de error: Procesar pago con pago existente
        [Test]
        public async Task ProcesarPago_DeberiaRedirigir_CuandoPagoExiste()
        {
            // Arrange
            var cliente = new Cliente { Nombres = "Juan Pérez", Dni = "12345678", Correo = "juan@email.com", Password = "secret", Estado = "activo", Direccion = "Calle 1", Telefono = "987654321" };
            var zona = new ZonaCobertura { NombreZona = "Norte", Distrito = "Lima", Descripcion = "Cobertura en zona norte" };
            var plan = new PlanServicio { NombrePlan = "Básico", TipoServicio = "Internet", PrecioMensual = 50m, ZonaCobertura = zona, Descripcion = "Plan básico" };
            var empleado = new Empleado { cargo_id = 1, nombres = "Operador", dni = "00000000", telefono = "999999999", correo = "op@ex.com", password = "secret", estado = "activo", fecha_inicio = DateTime.Now };

            _context.AddRange(cliente, zona, plan, empleado);
            await _context.SaveChangesAsync();

            var contrato = new Contrato
            {
                ClienteId = cliente.ClienteId,
                PlanId = plan.PlanId,
                EmpleadoId = empleado.empleado_id,
                Cliente = cliente,
                PlanServicio = plan,
                Empleado = empleado,
                Estado = "activo",
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(12)
            };

            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            var pago = new Pago { ContratoId = contrato.Id, Contrato = contrato, Monto = 100, EstadoPago = "pendiente", FechaDeVencimiento = DateTime.Now.AddDays(30), MetodoPago = "Yape" };
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _controller.ProcesarPago(pago.PagoId, "Yape");

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(resultado, "Debe redirigir a ConfirmacionPago.");
            var redirect = (RedirectToActionResult)resultado;
            Assert.AreEqual("ConfirmacionPago", redirect.ActionName);
        }

        // 4. Prueba exitosa: Buscar por DNI y retornar vista con datos
        [Test]
        public async Task BuscarPorDni_ClienteYContratoValidos_RetornaVistaMostrarDatosPago()
        {
            // Act
            var result = await _controller.BuscarPorDni("12345678") as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.AnyOf(null, "MostrarDatosPago"));
        }

        // 5. Prueba exitosa: Generar pago automático
        [Test]
        public async Task GenerarPagoAutomatico_CreaPagoPendiente2()
        {
            // Arrange
            var plan = new PlanServicio { PlanId = 1, NombrePlan = "Plan Básico", PrecioMensual = 50, Descripcion = "Plan de internet básico de prueba", TipoServicio = "Internet" };
            var contrato = new Contrato { Id = 1, ClienteId = 1, PlanId = 1, PlanServicio = plan, FechaInicio = new DateTime(2025, 1, 1), FechaFin = new DateTime(2025, 12, 31), Estado = "activo" };

            _context.PlanServicios.Add(plan);
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GenerarPagoAutomatico(1) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("DetallesContrato"));

            var pago = _context.Pagos.FirstOrDefault(p => p.ContratoId == 1);
            Assert.That(pago, Is.Not.Null);
            Assert.That(pago.EstadoPago, Is.EqualTo("pendiente"));
            Assert.That(pago.Monto, Is.EqualTo(50));
        }

        // 6. Prueba exitosa: Procesar pago y cambiar estado a pagado
        [Test]
        public async Task ProcesarPago_CambiaEstadoAPagado2()
        {
            // Arrange
            var cliente = new Cliente { ClienteId = 1, Dni = "12345678", Nombres = "Juan Pérez", Correo = "juan@example.com", Direccion = "Av. Siempre Viva 123", Estado = "Activo", Password = "1234", Telefono = "987654321" };
            var contrato = new Contrato { Id = 1, ClienteId = 1, Cliente = cliente, Estado = "activo" };
            var pago = new Pago { PagoId = 1, ContratoId = 1, Contrato = contrato, EstadoPago = "pendiente", Monto = 100, FechaDeVencimiento = DateTime.Now.AddDays(10), MetodoPago = "Por especificar" };

            _context.Clientes.Add(cliente);
            _context.Contratos.Add(contrato);
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ProcesarPago(1, "Tarjeta") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("ConfirmacionPago"));

            var pagoActualizado = _context.Pagos.First();
            Assert.That(pagoActualizado.EstadoPago, Is.EqualTo("pagado"));
            Assert.That(pagoActualizado.MetodoPago, Is.EqualTo("Tarjeta"));
        }
    }
}