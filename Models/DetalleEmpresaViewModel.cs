namespace Web_Recocycle.Models
{
    public class DetalleEmpresaViewModel
    {
        public string NombreEmpresa { get; set; }
        public string Correo { get; set; }
        public int Estado { get; set; }
        public byte[] ImgLogo { get; set; }
        public int TotalClientes { get; set; }
        public List<Reciclador> Clientes { get; set; }
    }


}
