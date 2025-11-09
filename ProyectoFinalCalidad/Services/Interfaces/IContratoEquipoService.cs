using System.Collections.Generic;
using System.Threading.Tasks;
using ProyectoFinalCalidad.Models;

namespace ProyectoFinalCalidad.Services.Interfaces
{
    public interface IContratoEquipoService
    {
        Task AsignarContratoEquipoAsync(int contratoId, int equipoId, string estado);
        Task AsignarUnidadEspecificaAsync(int contratoId, int equipoId, int equipoUnidadId, string estado);
        Task<List<ContratoEquipo>> ListarAsignacionesAsync();
        Task CambiarEstadoAsignacionAsync(int contratoEquipoId, string nuevoEstado);
    }
}
