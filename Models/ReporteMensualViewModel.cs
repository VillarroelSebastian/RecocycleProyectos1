namespace Web_Recocycle.Models
{
    public class ReporteMensualViewModel
    {
        public List<RecicladorReporteDTO> Recicladores { get; set; }
        public List<EmpresaReporteDTO> Empresas { get; set; }

        // Nuevas métricas
        public int TotalUsuariosActivos { get; set; }
        public int TotalReciclajesFinalizados { get; set; }
        public int TotalPublicaciones { get; set; }
        public int EmpresasActivas { get; set; }

        public Premio Premio { get; set; } // Premio vigente

        public List<MaterialDistribucionDTO> Materiales { get; set; } // Distribución de materiales
        public List<ReseñaDistribucionDTO> Reseñas { get; set; } // Distribución de reseñas

        // Comparativas
        public double CrecimientoReciclajes { get; set; }
        public double CrecimientoPublicaciones { get; set; }
        public double CrecimientoEmpresas { get; set; }
    }

    public class MaterialDistribucionDTO
    {
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ReseñaDistribucionDTO
    {
        public int NumEstrella { get; set; }
        public int Total { get; set; }
    }
}
