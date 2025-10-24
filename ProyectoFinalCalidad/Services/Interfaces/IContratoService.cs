namespace ProyectoFinalCalidad.Services
{
    using ProyectoFinalCalidad.Models;

    public interface IContratoService
    {
        Task<List<Contrato>> ObtenerTodosAsync();
        Task<Contrato?> ObtenerPorIdAsync(int id);
        Task AgregarAsync(Contrato contrato);
        Task ActualizarAsync(Contrato contrato);
    }
}