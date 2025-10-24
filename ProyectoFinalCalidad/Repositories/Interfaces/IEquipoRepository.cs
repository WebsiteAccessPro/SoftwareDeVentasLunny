using ProyectoFinalCalidad.Models;

namespace ProyectoFinalCalidad.Repositories.Interfaces
{

    public interface IEquipoRepository
    {
        Task<List<Equipo>> GetAllAsync();
        Task<Equipo> GetByIdAsync(int id);
        Task AddAsync(Equipo equipo);
        Task UpdateAsync(Equipo equipo);
        Task IncreaseStockAsync(int equipoId, int cantidad);
    }
}
