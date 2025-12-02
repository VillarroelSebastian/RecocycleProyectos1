using System.Data.SqlClient;
using Web_Recocycle.Models;

public class ReporteHelper
{
    private readonly string _connectionString;

    public ReporteHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Usuarios activos
    public int ObtenerUsuariosActivos(int mes, int año)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();
        string query = @"SELECT COUNT(*) 
                         FROM Usuario 
                         WHERE estado = 1 
                           AND MONTH(fechaRegistro) = @mes 
                           AND YEAR(fechaRegistro) = @año";
        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);
        return (int)cmd.ExecuteScalar();
    }

    // Reciclajes finalizados (ejemplo, si tienes tabla Asignacion)
    public int ObtenerReciclajesFinalizados(int mes, int año)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();
        string query = @"SELECT COUNT(*) 
                     FROM Asignacion 
                     WHERE estado = 'Finalizado'
                       AND MONTH(fecha) = @mes 
                       AND YEAR(fecha) = @año";
        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);
        return (int)cmd.ExecuteScalar();
    }


    // Empresas activas
    public int ObtenerEmpresasActivas(int mes, int año)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();
        string query = @"SELECT COUNT(*) 
                         FROM EmpresaRecolector 
                         WHERE estado = 1 
                           AND MONTH(fechaRegistro) = @mes 
                           AND YEAR(fechaRegistro) = @año";
        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);
        return (int)cmd.ExecuteScalar();
    }

    // Distribución de materiales reciclados
    public List<MaterialDistribucionDTO> ObtenerDistribucionMateriales(int mes, int año)
    {
        var lista = new List<MaterialDistribucionDTO>();
        using var con = new SqlConnection(_connectionString);
        con.Open();

        string query = @"
        SELECT c.nombre AS TipoMaterial, COUNT(*) AS Cantidad
        FROM RegistroReciclaje rr
        INNER JOIN Categoria c ON rr.idCategoria = c.idCategoria
        WHERE MONTH(rr.fechaRegistro) = @mes 
          AND YEAR(rr.fechaRegistro) = @año
        GROUP BY c.nombre";

        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);

        using var reader = cmd.ExecuteReader();
        int total = 0;
        var temp = new List<(string tipo, int cantidad)>();

        while (reader.Read())
        {
            string tipo = reader.GetString(0);   // nombre de la categoría
            int cantidad = reader.GetInt32(1);
            total += cantidad;
            temp.Add((tipo, cantidad));
        }

        foreach (var t in temp)
        {
            lista.Add(new MaterialDistribucionDTO
            {
                Tipo = t.tipo,
                Cantidad = t.cantidad,
                Porcentaje = total > 0 ? Math.Round((double)t.cantidad / total * 100, 2) : 0
            });
        }

        return lista;
    }



    // Distribución de reseñas
    public List<ReseñaDistribucionDTO> ObtenerDistribucionReseñas(int mes, int año)
    {
        var lista = new List<ReseñaDistribucionDTO>();
        using var con = new SqlConnection(_connectionString);
        con.Open();
        string query = @"SELECT numEstrella, COUNT(*) AS Total
                         FROM Reseña
                         WHERE MONTH(fechaReseña) = @mes 
                           AND YEAR(fechaReseña) = @año
                         GROUP BY numEstrella";
        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new ReseñaDistribucionDTO
            {
                NumEstrella = reader.GetInt32(0),
                Total = reader.GetInt32(1)
            });
        }
        return lista;
    }

    public List<RecicladorReporteDTO> ObtenerDatosRecicladoresReporte(int mes, int año)
    {
        var lista = new List<RecicladorReporteDTO>();
        using var con = new SqlConnection(_connectionString);
        con.Open();

        string query = @"
        SELECT r.nombres, r.apellidos,
               r.puntos AS Puntos,
               ISNULL(COUNT(rr.idRegistroReciclaje), 0) AS TotalReciclajes,
               ISNULL(AVG(CAST(rs.numEstrella AS FLOAT)), 0) AS PromedioEstrellas
        FROM Reciclador r
        LEFT JOIN RegistroReciclaje rr ON rr.idUsuario = r.idUsuario
            AND MONTH(rr.fechaRegistro) = @mes AND YEAR(rr.fechaRegistro) = @año
        LEFT JOIN Reseña rs ON rs.idUsuario = r.idUsuario
            AND MONTH(rs.fechaReseña) = @mes AND YEAR(rs.fechaReseña) = @año
        GROUP BY r.nombres, r.apellidos, r.puntos
        ORDER BY Puntos DESC";

        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new RecicladorReporteDTO
            {
                Nombres = reader.GetString(0),
                Apellidos = reader.GetString(1),
                Puntos = reader.GetInt32(2),
                TotalReciclajes = reader.GetInt32(3),
                PromedioEstrellas = reader.IsDBNull(4) ? 0 : Math.Round(Convert.ToDouble(reader[4]), 2)
            });
        }

        return lista;
    }



    public List<EmpresaReporteDTO> ObtenerDatosEmpresasReporte(int mes, int año)
    {
        var lista = new List<EmpresaReporteDTO>();
        using var con = new SqlConnection(_connectionString);
        con.Open();

        string query = @"
        SELECT e.nombreEmpresa, u.correo, e.telefono, e.fechaRegistro,
               COUNT(a.idAsignacion) AS TotalAsignaciones
        FROM EmpresaRecolector e
        INNER JOIN Usuario u ON u.idUsuario = e.idUsuario
        LEFT JOIN Asignacion a ON a.idRecolector = e.idUsuario
            AND a.estado = 'Finalizado'
            AND MONTH(a.fecha) = @mes AND YEAR(a.fecha) = @año
        GROUP BY e.nombreEmpresa, u.correo, e.telefono, e.fechaRegistro
        ORDER BY TotalAsignaciones DESC";

        using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@mes", mes);
        cmd.Parameters.AddWithValue("@año", año);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new EmpresaReporteDTO
            {
                NombreEmpresa = reader.IsDBNull(0) ? "" : reader.GetString(0),
                Correo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Telefono = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                FechaRegistro = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                TotalAsignaciones = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            });
        }

        return lista;
    }

}



