using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("Zona_cobertura")]
    public class ZonaCobertura
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nombre_zona")]
        [StringLength(100)]
        public string NombreZona { get; set; }

        [Required]
        [Column("distrito")]
        [StringLength(100)]
        public string Distrito { get; set; }

        [Column("descripcion")]
        [StringLength(255)]
        public string Descripcion { get; set; }

        public ICollection<PlanServicio> PlanesServicio { get; set; } = new List<PlanServicio>();
    }
}
