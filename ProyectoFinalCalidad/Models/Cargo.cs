using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    public class Cargo
    {
        [Key]
        public int cargo_id { get; set; }
        [Required(ErrorMessage = "El cargo es obligatoria.")]
        public string titulo_cargo { get; set; }

        public ICollection<Empleado> Empleados { get; set; }
    }

}
