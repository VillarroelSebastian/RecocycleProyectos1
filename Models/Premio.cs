using System.ComponentModel.DataAnnotations;

namespace Web_Recocycle.Models
{
    public class Premio
    {
        [Key]
        public int IdPremio { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaLimite { get; set; }
        public byte[] ImgPremio { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? ModificadoPor { get; set; }
        public int IdUsuario { get; set; }
    }
}
