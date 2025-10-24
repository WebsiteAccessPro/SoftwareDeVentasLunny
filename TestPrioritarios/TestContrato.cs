using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Threading.Tasks;

namespace TestPrioritarios
{
    [TestFixture]
    public class TestContrato
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
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        // 1. Prueba: Buscar por DNI inexistente
        [Test]
        public async Task BuscarPorDni_DeberiaMostrarMensaje_CuandoDniNoExiste()
        {
            // Act
            var resultado = await _controller.BuscarPorDni("99999999") as ViewResult;

            // Assert
            Assert.IsNotNull(resultado, "Debe retornar una vista.");
            Assert.AreEqual("DNI no registrado.", resultado.ViewData["Mensaje"]);
        }

        // 2. Prueba: GenerarPagoAutomatico con contrato inexistente
        [Test]
        public async Task GenerarPagoAutomatico_DeberiaRetornarNotFound_CuandoContratoNoExiste()
        {
            // Act
            var resultado = await _controller.GenerarPagoAutomatico(999) as NotFoundObjectResult;

            // Assert
            Assert.IsNotNull(resultado, "Debe retornar un resultado NotFound.");
            Assert.AreEqual("Contrato no encontrado", resultado.Value);
        }

        // 3. Prueba: Checkout con pago inexistente
        [Test]
        public void Checkout_DeberiaRetornarNotFound_CuandoPagoNoExiste()
        {
            // Act
            var resultado = _controller.Checkout(999);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(resultado, "Debe retornar NotFound si el pago no existe.");
        }
    }
}
