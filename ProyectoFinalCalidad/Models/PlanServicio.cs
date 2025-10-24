using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("Plan_servicio")]
    public class PlanServicio
    {
        [Key]
        [Column("plan_id")]
        public int PlanId { get; set; }

        [Required]
        [Column("nombre_plan")]
        [StringLength(100)]
        public string NombrePlan { get; set; }

        [Required]
        [Column("tipo_servicio")]
        [StringLength(50)]
        public string TipoServicio { get; set; }

        [Column("velocidad")]
        public int? Velocidad { get; set; }

        [Column("descripcion")]
        [StringLength(255)]
        public string Descripcion { get; set; }

        [Required]
        [Column("precio_mensual")]
        public decimal PrecioMensual { get; set; }

        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "activo";

        [Required]
        [Column("zona_id")]
        public int ZonaId { get; set; }

        [ForeignKey(nameof(ZonaId))]
        public ZonaCobertura ZonaCobertura { get; set; }

        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    }
}
