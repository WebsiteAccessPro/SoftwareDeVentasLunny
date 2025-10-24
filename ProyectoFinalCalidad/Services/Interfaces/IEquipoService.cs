using ProyectoFinalCalidad.Models;
using Microsoft.EntityFrameworkCore;


namespace ProyectoFinalCalidad.Services.Interfaces
{

    public interface IEquipoService
    {
        Task<List<Equipo>> ListarAsync();
        Task<Equipo> BuscarPorIdAsync(int id);
        Task CrearAsync(Equipo equipo);
        Task AumentarStockAsync(int equipoId, int cantidad);
    }

}
