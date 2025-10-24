using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Services;
using ProyectoFinalCalidad.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestPrioritarios
{
    [TestFixture]
    public class TestContrato1
    {
        private Mock<IContratoService> _mockContratoService;

        [SetUp]
        public void Setup()
        {
            _mockContratoService = new Mock<IContratoService>();
        }

        // 1. Prueba de éxito: Obtener todos los contratos
        [Test]
        public async Task ObtenerTodosAsync_DeberiaRetornarListaContratos()
        {
            // Arrange
            var contratosSimulados = new List<Contrato>
            {
                new Contrato { Id = 1, ClienteId = 101, PlanId = 5, EmpleadoId = 11, FechaInicio = new DateTime(2025,1,1), FechaFin = new DateTime(2026,1,1), Estado = "activo" },
                new Contrato { Id = 2, ClienteId = 102, PlanId = 6, EmpleadoId = 12, FechaInicio = new DateTime(2025,3,1), FechaFin = new DateTime(2026,3,1), Estado = "inactivo" }
            };

            _mockContratoService.Setup(s => s.ObtenerTodosAsync()).ReturnsAsync(contratosSimulados);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerTodosAsync();

            // Assert
            Assert.IsNotNull(resultado, "La lista de contratos no debe ser nula.");
            Assert.AreEqual(2, resultado.Count, "Debe retornar 2 contratos simulados.");
            Assert.AreEqual("activo", resultado[0].Estado);
            Assert.AreEqual(101, resultado[0].ClienteId);
        }

        // 2. Prueba de éxito: Obtener contrato por Id
        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarContratoCorrecto()
        {
            // Arrange
            var contratoSimulado = new Contrato
            {
                Id = 99,
                ClienteId = 500,
                PlanId = 8,
                EmpleadoId = 20,
                FechaInicio = new DateTime(2025, 5, 1),
                FechaFin = new DateTime(2026, 5, 1),
                Estado = "activo"
            };

            _mockContratoService.Setup(s => s.ObtenerPorIdAsync(99)).ReturnsAsync(contratoSimulado);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerPorIdAsync(99);

            // Assert
            Assert.IsNotNull(resultado);
            Assert.AreEqual(99, resultado.Id);
            Assert.AreEqual(500, resultado.ClienteId);
            Assert.AreEqual("activo", resultado.Estado);
        }

        // 3. Prueba de éxito: Agregar contrato
        [Test]
        public async Task AgregarAsync_DeberiaEjecutarseCorrectamente()
        {
            // Arrange
            var nuevoContrato = new Contrato
            {
                Id = 3,
                ClienteId = 200,
                PlanId = 10,
                EmpleadoId = 30,
                FechaInicio = new DateTime(2025, 10, 1),
                FechaFin = new DateTime(2026, 10, 1),
                Estado = "activo"
            };

            _mockContratoService.Setup(s => s.AgregarAsync(It.IsAny<Contrato>())).Returns(Task.CompletedTask).Verifiable();

            // Act
            await _mockContratoService.Object.AgregarAsync(nuevoContrato);

            // Assert
            _mockContratoService.Verify(
                s => s.AgregarAsync(It.Is<Contrato>(c => c.Id == 3 && c.ClienteId == 200)),
                Times.Once
            );
        }

        // 4. Prueba de éxito: Actualizar contrato
        [Test]
        public async Task ActualizarAsync_DeberiaActualizarContrato()
        {
            // Arrange
            var contratoExistente = new Contrato
            {
                Id = 4,
                ClienteId = 300,
                PlanId = 15,
                EmpleadoId = 40,
                Estado = "activo"
            };

            _mockContratoService.Setup(s => s.ActualizarAsync(It.IsAny<Contrato>())).Returns(Task.CompletedTask).Verifiable();

            // Act
            contratoExistente.Estado = "inactivo";
            await _mockContratoService.Object.ActualizarAsync(contratoExistente);

            // Assert
            _mockContratoService.Verify(
                s => s.ActualizarAsync(It.Is<Contrato>(c => c.Estado == "inactivo")),
                Times.Once
            );
        }

        // 5. Prueba de error: Obtener contrato inexistente
        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_SiNoExiste()
        {
            // Arrange
            _mockContratoService.Setup(s => s.ObtenerPorIdAsync(It.IsAny<int>())).ReturnsAsync((Contrato)null);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerPorIdAsync(999);

            // Assert
            Assert.IsNull(resultado, "Debe devolver null si no se encuentra el contrato.");
        }

        // 6. Prueba de error: Agregar contrato nulo
        [Test]
        public void AgregarAsync_DeberiaLanzarExcepcion_SiContratoEsNull()
        {
            // Arrange
            _mockContratoService.Setup(s => s.AgregarAsync(null)).ThrowsAsync(new ArgumentNullException("contrato"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _mockContratoService.Object.AgregarAsync(null));
        }
    }
}
