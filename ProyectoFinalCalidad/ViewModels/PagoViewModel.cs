using System.Collections.Generic;

namespace ProyectoFinalCalidad.Models.ViewModels
{
    public class PagoViewModel
    {
        public Cliente Cliente { get; set; }
        public Contrato Contrato { get; set; }
        public List<Pago> Pagos { get; set; }
    }
}
