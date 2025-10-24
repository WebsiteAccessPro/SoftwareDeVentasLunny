using ProyectoFinalCalidad.Models;

namespace ProyectoFinalCalidad.Services.Interfaces
{
    public interface IClienteService
    {
        Task<List<Contrato>> ObtenerContratosPorClienteAsync(int usuarioId);
        Task<List<Pago>> ObtenerPagosPendientesAsync(int clienteId);
        Task<bool> RegistrarPagoAsync(int pagoId, string metodoPago);
    }

}
