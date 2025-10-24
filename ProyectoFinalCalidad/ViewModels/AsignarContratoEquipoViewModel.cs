using System.ComponentModel.DataAnnotations;

namespace ProyectoFinalCalidad.ViewModels.ContratoEquipo
{
    public class AsignarContratoEquipoViewModel
    {
        [Required]
        public int ContratoId { get; set; }

        [Required]
        public int EquipoId { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; }
    }
}
