using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestPruebasUnitarias
{
    [TestFixture]
    public class ContratoServiceTests
    {
        private Mock<IContratoService> _mockContratoService;

        [SetUp]
        public void Setup()
        {
            _mockContratoService = new Mock<IContratoService>();
        }

        [Test]
        public async Task ObtenerTodosAsync_DeberiaRetornarListaContratos()
        {
            // Arrange
            var contratosSimulados = new List<Contrato>
            {
                new Contrato { Id = 1, ClienteId = 101, PlanId = 5, EmpleadoId = 11, Estado = "activo" },
                new Contrato { Id = 2, ClienteId = 102, PlanId = 6, EmpleadoId = 12, Estado = "inactivo" }
            };

            _mockContratoService
                .Setup(s => s.ObtenerTodosAsync())
                .ReturnsAsync(contratosSimulados);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerTodosAsync();

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado.Count, Is.EqualTo(2));
            Assert.That(resultado[0].Estado, Is.EqualTo("activo"));
        }

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarContratoCorrecto()
        {
            // Arrange
            var contratoSimulado = new Contrato { Id = 99, ClienteId = 500, Estado = "activo" };

            _mockContratoService
                .Setup(s => s.ObtenerPorIdAsync(99))
                .ReturnsAsync(contratoSimulado);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerPorIdAsync(99);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado.Id, Is.EqualTo(99));
            Assert.That(resultado.ClienteId, Is.EqualTo(500));
        }

        [Test]
        public async Task AgregarAsync_DeberiaEjecutarseCorrectamente()
        {
            // Arrange
            var nuevoContrato = new Contrato { Id = 3, ClienteId = 200 };
            _mockContratoService
                .Setup(s => s.AgregarAsync(It.IsAny<Contrato>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _mockContratoService.Object.AgregarAsync(nuevoContrato);

            // Assert
            _mockContratoService.Verify(
                s => s.AgregarAsync(It.Is<Contrato>(c => c.Id == 3 && c.ClienteId == 200)),
                Times.Once);
        }

        [Test]
        public async Task ActualizarAsync_DeberiaActualizarContrato()
        {
            // Arrange
            var contrato = new Contrato { Id = 4, Estado = "activo" };
            _mockContratoService
                .Setup(s => s.ActualizarAsync(It.IsAny<Contrato>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            contrato.Estado = "inactivo";
            await _mockContratoService.Object.ActualizarAsync(contrato);

            // Assert
            _mockContratoService.Verify(
                s => s.ActualizarAsync(It.Is<Contrato>(c => c.Estado == "inactivo")),
                Times.Once);
        }

        [Test]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_SiNoExiste()
        {
            // Arrange
            _mockContratoService
                .Setup(s => s.ObtenerPorIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Contrato)null);

            // Act
            var resultado = await _mockContratoService.Object.ObtenerPorIdAsync(999);

            // Assert
            Assert.That(resultado, Is.Null);
        }

        [Test]
        public void AgregarAsync_DeberiaLanzarExcepcion_SiContratoEsNull()
        {
            // Arrange
            _mockContratoService
                .Setup(s => s.AgregarAsync(null))
                .ThrowsAsync(new ArgumentNullException("contrato"));

            // Act & Assert
            Assert.That(async () => await _mockContratoService.Object.AgregarAsync(null),
                        Throws.TypeOf<ArgumentNullException>());
        }
    }
}