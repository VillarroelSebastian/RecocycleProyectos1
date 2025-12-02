namespace Web_Recocycle.Models
{
    public class EmpresaReporteDTO
    {
        public string NombreEmpresa { get; set; }
        public string Correo { get; set; }
        public int Telefono { get; set; } // string si es varchar
        public DateTime FechaRegistro { get; set; }
        public int TotalAsignaciones { get; set; }
        public double PromedioEstrellas { get; set; }
    }

    public class RecicladorReporteDTO
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public int Puntos { get; set; }
        public double PromedioEstrellas { get; set; }
        public int TotalReciclajes { get; set; }
    }

    

}
