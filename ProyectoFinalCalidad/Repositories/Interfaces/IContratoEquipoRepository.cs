using System.Collections.Generic;
using System.Threading.Tasks;
using ProyectoFinalCalidad.Models;

namespace ProyectoFinalCalidad.Repositories.Interfaces
{
    public interface IContratoEquipoRepository
    {
        Task<List<ContratoEquipo>> ListarAsync();
        Task<ContratoEquipo> BuscarPorIdAsync(int id);
        Task CrearAsync(ContratoEquipo contratoEquipo);
        Task ActualizarAsync(ContratoEquipo contratoEquipo);
        Task EliminarAsync(int id);
    }
}
