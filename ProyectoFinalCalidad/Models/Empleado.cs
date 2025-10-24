using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    public class Empleado
    {
        [Key]
        public int empleado_id { get; set; }

        public int cargo_id { get; set; }

        public string? nombres { get; set; }
        public string? dni { get; set; }
        public string? telefono { get; set; }
        public string? correo { get; set; }
        public string? password { get; set; }
        public string? estado { get; set; }

        public DateTime? fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }

        [ForeignKey("cargo_id")]
        public Cargo? Cargo { get; set; }
    }

}
