using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text;

namespace Web_Recocycle.Models

{
    public class Empresa
    {
        [Key]
        public int IdUsuario { get; set; }
        public string NombreEmpresa { get; set; }
        public byte[] ImgLogo { get; set; }
        public int IdCategoria { get; set; }
        public string Categorias { get; set; }


        public int Telefono { get; set; }
        public string Correo { get; set; }
        public string Contrasenia { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int Estado { get; set; }
        public int Rol { get; set; }


        public List<int> CategoriasSeleccionadas { get; set; } = new List<int>();


    }

    public class Asignacion
    {
        public int IdRegistroReciclaje { get; set; }
        public int IdRecolector { get; set; }
        public int IdReciclador { get; set; }
        public string Fecha { get; set; }
        public string DesdeHora { get; set; }
        public string HastaHora { get; set; }
    }
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }
        public string Nombre { get; set; }
    }


    public class EmpresaHelper
    {
        private readonly string _connectionString;

        public EmpresaHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int RegistrarUsuario(Empresa empresa)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"INSERT INTO Usuario (correo, telefono, contrasenia, fechaRegistro, fechaModificacion, estado, idRol)
                         OUTPUT INSERTED.idUsuario
                         VALUES (@correo, @telefono, @contrasenia, GETDATE(), GETDATE(), @estado, @rol)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@correo", empresa.Correo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@telefono", empresa.Telefono);
                    cmd.Parameters.AddWithValue("@contrasenia", HashPassword(empresa.Contrasenia ?? ""));
                    cmd.Parameters.AddWithValue("@estado", empresa.Estado);
                    cmd.Parameters.AddWithValue("@rol", empresa.Rol);


                    return (int)cmd.ExecuteScalar();
                }
            }
        }
        public List<EmpresaReporteDTO> ObtenerDatosEmpresasReporte(string mes)
        {
            var empresas = new List<EmpresaReporteDTO>();
            var query = @"
            SELECT 
                e.nombreEmpresa,
                u.correo AS correo,
                u.telefono AS telefono,
                u.fechaRegistro AS fechaRegistro,
                COUNT(a.idAsignacion) AS TotalAsignaciones,
                ISNULL(AVG(CAST(r.numEstrella AS FLOAT)), 0) AS PromedioEstrellas
            FROM EmpresaRecolector e
            INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
            LEFT JOIN Asignacion a ON e.idUsuario = a.idReciclador
            LEFT JOIN Reseña r ON e.idUsuario = r.idUsuario
            WHERE MONTH(a.fecha) = @Mes
            GROUP BY e.nombreEmpresa, u.correo, u.telefono, u.fechaRegistro;
        ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@Mes", Convert.ToInt32(mes));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        empresas.Add(new EmpresaReporteDTO
                        {
                            NombreEmpresa = reader["nombreEmpresa"].ToString(),
                            Correo = reader["correo"].ToString(),
                            Telefono = Convert.ToInt32(reader["telefono"]),
                            FechaRegistro = Convert.ToDateTime(reader["fechaRegistro"]),
                            TotalAsignaciones = Convert.ToInt32(reader["TotalAsignaciones"]),
                            PromedioEstrellas = Convert.ToDouble(reader["PromedioEstrellas"])
                        });
                    }
                }
            }
            return empresas;
        }

        public bool RegistrarEmpresaRecolector(Empresa empresa)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string queryEmpresa = @"
        INSERT INTO EmpresaRecolector (
            idUsuario, nombreEmpresa, imgLogo, telefono,
            fechaRegistro, fechaModificacion, estado, rol
        )
        VALUES (
            @idUsuario, @nombreEmpresa, @imgLogo, @telefono,
            @fechaRegistro, @fechaModificacion, @estado, @rol
        )";

                using (SqlCommand cmdEmpresa = new SqlCommand(queryEmpresa, con))
                {
                    cmdEmpresa.Parameters.AddWithValue("@idUsuario", empresa.IdUsuario);
                    cmdEmpresa.Parameters.AddWithValue("@nombreEmpresa", empresa.NombreEmpresa);
                    cmdEmpresa.Parameters.AddWithValue("@imgLogo", empresa.ImgLogo ?? (object)DBNull.Value);
                    cmdEmpresa.Parameters.AddWithValue("@telefono", empresa.Telefono);
                    cmdEmpresa.Parameters.AddWithValue("@fechaRegistro", empresa.FechaRegistro);
                    cmdEmpresa.Parameters.AddWithValue("@fechaModificacion", empresa.FechaModificacion);
                    cmdEmpresa.Parameters.AddWithValue("@estado", empresa.Estado);
                    cmdEmpresa.Parameters.AddWithValue("@rol", empresa.Rol);

                    int rowsEmpresa = cmdEmpresa.ExecuteNonQuery();
                    return rowsEmpresa > 0;
                }
            }
        }


        public void RegistrarEmpresaCategoria(int idUsuario, int idCategoria)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"INSERT INTO EmpresaCategoria (idUsuario, idCategoria)
                         VALUES (@idUsuario, @idCategoria)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@idCategoria", idCategoria);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        public List<Categoria> ObtenerCategorias()
        {
            var lista = new List<Categoria>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "SELECT idCategoria, nombre FROM Categoria";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Categoria
                        {
                            IdCategoria = reader.GetInt32(0),
                            Nombre = reader.GetString(1)
                        });
                    }
                }
            }

            return lista;
        }

        public Empresa ObtenerDatosBasicos(int idUsuario)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
            SELECT e.nombreEmpresa, u.correo, u.estado, e.imgLogo
FROM EmpresaRecolector e
INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
WHERE e.idUsuario = @idUsuario
";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Empresa
                            {
                                NombreEmpresa = reader.GetString(0),
                                Correo = reader.GetString(1),
                                Estado = reader.GetBoolean(2) ? 1 : 0,
                                ImgLogo = reader.IsDBNull(3) ? null : (byte[])reader["imgLogo"]
                            };

                        }
                    }
                }
            }

            return null;
        }


        public List<Empresa> ObtenerEmpresas()
        {
            var lista = new List<Empresa>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
           SELECT 
    e.idUsuario,
    e.nombreEmpresa,
    e.imgLogo,
    STRING_AGG(c.nombre, ', ') AS categorias,
    u.correo,
    u.telefono,
    u.contrasenia,
    u.fechaRegistro,
    u.fechaModificacion,
    u.estado,
    u.idRol
FROM EmpresaRecolector e
INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
LEFT JOIN EmpresaCategoria ec ON e.idUsuario = ec.idUsuario
LEFT JOIN Categoria c ON ec.idCategoria = c.idCategoria
WHERE u.idRol = 2 AND u.estado = 1
GROUP BY 
    e.idUsuario, e.nombreEmpresa, e.imgLogo,
    u.correo, u.telefono, u.contrasenia,
    u.fechaRegistro, u.fechaModificacion, u.estado, u.idRol
";


                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var empresa = new Empresa
                        {
                            IdUsuario = reader.GetInt32(reader.GetOrdinal("idUsuario")),
                            NombreEmpresa = reader.GetString(reader.GetOrdinal("nombreEmpresa")),
                            ImgLogo = reader.IsDBNull(reader.GetOrdinal("imgLogo")) ? null : (byte[])reader["imgLogo"],
                            Categorias = reader.IsDBNull(reader.GetOrdinal("categorias")) ? "Sin categoría" : reader.GetString(reader.GetOrdinal("categorias")),
                            Correo = reader.IsDBNull(reader.GetOrdinal("correo")) ? "Sin correo" : reader.GetString(reader.GetOrdinal("correo")),
                            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? 0 : reader.GetInt32(reader.GetOrdinal("telefono")),
                            Contrasenia = reader.IsDBNull(reader.GetOrdinal("contrasenia")) ? "Sin contraseña" : reader.GetString(reader.GetOrdinal("contrasenia")),
                            FechaRegistro = reader.IsDBNull(reader.GetOrdinal("fechaRegistro")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fechaRegistro")),
                            FechaModificacion = reader.IsDBNull(reader.GetOrdinal("fechaModificacion")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("fechaModificacion")),
                            Estado = reader.IsDBNull(reader.GetOrdinal("estado")) ? 0 : (reader.GetBoolean(reader.GetOrdinal("estado")) ? 1 : 0),
                            Rol = reader.GetInt32(reader.GetOrdinal("idRol"))
                        };

                        lista.Add(empresa);
                    }
                }
            }

            return lista;
        }


        //Eliminar
        public void CambiarEstadoEmpresa(int idUsuario, int nuevoEstado)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                // Cambiar estado en EmpresaRecolector
                string queryEmpresa = "UPDATE EmpresaRecolector SET estado = @estado WHERE idUsuario = @idUsuario";
                using (SqlCommand cmdEmpresa = new SqlCommand(queryEmpresa, con))
                {
                    cmdEmpresa.Parameters.AddWithValue("@estado", nuevoEstado); // 0 = inactivo, 1 = activo
                    cmdEmpresa.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmdEmpresa.ExecuteNonQuery();
                }

                // Cambiar estado en Usuario también
                string queryUsuario = "UPDATE Usuario SET estado = @estado WHERE idUsuario = @idUsuario";
                using (SqlCommand cmdUsuario = new SqlCommand(queryUsuario, con))
                {
                    cmdUsuario.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmdUsuario.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmdUsuario.ExecuteNonQuery();
                }
            }
        }




        public void ActualizarEmpresa(Empresa empresa)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                // Actualizar datos en EmpresaRecolector
                string queryEmpresa = @"UPDATE EmpresaRecolector SET
                                nombreEmpresa = @nombreEmpresa,
                                imgLogo = @imgLogo
                                WHERE idUsuario = @idUsuario";

                using (SqlCommand cmd = new SqlCommand(queryEmpresa, con))
                {
                    cmd.Parameters.AddWithValue("@nombreEmpresa", empresa.NombreEmpresa);
                    cmd.Parameters.AddWithValue("@imgLogo", empresa.ImgLogo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@idUsuario", empresa.IdUsuario);
                    cmd.ExecuteNonQuery();
                }

                // Actualizar datos en Usuario
                string queryUsuario = @"UPDATE Usuario SET
                                correo = @correo,
                                telefono = @telefono,
                                fechaModificacion = GETDATE()
                                WHERE idUsuario = @idUsuario";

                using (SqlCommand cmd = new SqlCommand(queryUsuario, con))
                {
                    cmd.Parameters.AddWithValue("@correo", empresa.Correo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@telefono", empresa.Telefono);
                    cmd.Parameters.AddWithValue("@idUsuario", empresa.IdUsuario);
                    cmd.ExecuteNonQuery();
                }

                // Eliminar categorías anteriores
                string deleteCategorias = "DELETE FROM EmpresaCategoria WHERE idUsuario = @idUsuario";
                using (SqlCommand cmdDelete = new SqlCommand(deleteCategorias, con))
                {
                    cmdDelete.Parameters.AddWithValue("@idUsuario", empresa.IdUsuario);
                    cmdDelete.ExecuteNonQuery();
                }

                // Insertar nuevas categorías
                foreach (int idCat in empresa.CategoriasSeleccionadas)
                {
                    string insertCategoria = "INSERT INTO EmpresaCategoria (idUsuario, idCategoria) VALUES (@idUsuario, @idCategoria)";
                    using (SqlCommand cmdInsert = new SqlCommand(insertCategoria, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@idUsuario", empresa.IdUsuario);
                        cmdInsert.Parameters.AddWithValue("@idCategoria", idCat);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
            }
        }

        public Empresa ObtenerEmpresaPorId(int idUsuario)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                // Obtener datos de EmpresaRecolector y Usuario
                string query = @"
            SELECT 
                e.nombreEmpresa,
                e.imgLogo,
                u.correo,
                u.telefono
            FROM EmpresaRecolector e
            INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
            WHERE e.idUsuario = @idUsuario";

                Empresa empresa = null;

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            empresa = new Empresa
                            {
                                IdUsuario = idUsuario,
                                NombreEmpresa = reader.GetString(0),
                                ImgLogo = reader.IsDBNull(1) ? null : (byte[])reader["imgLogo"],
                                Correo = reader.IsDBNull(2) ? "Sin correo" : reader.GetString(2),
                                Telefono = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                CategoriasSeleccionadas = new List<int>()
                            };
                        }
                    }
                }

                // Obtener categorías asociadas
                string queryCategorias = "SELECT idCategoria FROM EmpresaCategoria WHERE idUsuario = @idUsuario";
                using (SqlCommand cmdCat = new SqlCommand(queryCategorias, con))
                {
                    cmdCat.Parameters.AddWithValue("@idUsuario", idUsuario);
                    using (SqlDataReader catReader = cmdCat.ExecuteReader())
                    {
                        while (catReader.Read())
                        {
                            empresa.CategoriasSeleccionadas.Add(catReader.GetInt32(0));
                        }
                    }
                }

                return empresa;
            }
        }
        public int ObtenerEmpresasActivas()
        {
            int cantidad = 0;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
            SELECT COUNT(*) 
            FROM Usuario u
            INNER JOIN EmpresaRecolector e ON u.idUsuario = e.idUsuario
            WHERE u.idRol = 2
              AND u.estado = 1
              AND MONTH(u.fechaRegistro) = MONTH(GETDATE())
              AND YEAR(u.fechaRegistro) = YEAR(GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cantidad = (int)cmd.ExecuteScalar();
                }
            }

            return cantidad;
        }




    }

    public class DatabaseHelper3
    {
        private readonly string _connectionString;

        public DatabaseHelper3(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public List<RegistrosReciclaje> ObtenerTodosLosRegistrosReciclaje()
        {
            var registrosVm = new List<RegistrosReciclaje>();

            string query = @"  SELECT 
                        rr.idRegistroReciclaje, 
                        c.nombre AS NombreCategoria, 
                        rr.descripcion, 
                        rr.fechaRegistro,
                        rr.latitud, 
                        rr.longitud, 
                        CONCAT(r.nombres, ' ', r.apellidos ) AS NombreReciclador,
                        rr.idUsuario,
                        rr.idCategoria
                    FROM RegistroReciclaje rr
                    INNER JOIN Categoria c ON c.idCategoria = rr.idCategoria
                    INNER JOIN Usuario u ON u.idUsuario = rr.idUsuario
                    INNER JOIN Reciclador r ON r.idUsuario = u.idUsuario
					INNER JOIN FechaDisponible fd ON fd.idRegistroReciclaje = rr.idRegistroReciclaje
                    WHERE rr.estado = 1 AND rr.modo = 'Libre' AND fd.fecha >= CURRENT_TIMESTAMP";


            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetInt32(0);

                            var registroVm = new RegistrosReciclaje
                            {
                                IdRegistroReciclaje = id,

                                NombreCategoria = reader.GetString(1),
                                NombreCompletoReciclador = reader.GetString(reader.GetOrdinal("NombreReciclador")),

                                Descripcion = reader.GetString(2),
                                FechaRegistro = reader.GetDateTime(3),
                                Latitud = reader.GetString(4),
                                Longitud = reader.GetString(5),
                                IdUsuario = reader.GetInt32(7),
                                Categoria = reader.GetInt32(8),

                                FechasDisponibles = ObtenerFechasDisponibles(id),
                                Imagenes = ObtenerImagenes(id),
                            };

                            registrosVm.Add(registroVm);
                        }
                    }
                }
            }
            return registrosVm;
        }

        public List<FechaDisponible> ObtenerFechasDisponibles(int idRegistroReciclaje)
        {
            var fechas = new List<FechaDisponible>();
            string query = @"SELECT fecha, desdeHora, hastaHora 
                            FROM FechaDisponible
                            WHERE idRegistroReciclaje = @id AND estado = 1";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", idRegistroReciclaje);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            DateTime fechaDb = reader.GetDateTime(reader.GetOrdinal("fecha"));
                            string fechaString = fechaDb.ToShortDateString();

                            TimeSpan desdeHoraDb = reader.GetTimeSpan(reader.GetOrdinal("desdeHora"));
                            TimeSpan hastaHoraDb = reader.GetTimeSpan(reader.GetOrdinal("hastaHora"));

                            string desdeHoraString = desdeHoraDb.ToString(@"hh\:mm");
                            string hastaHoraString = hastaHoraDb.ToString(@"hh\:mm");

                            fechas.Add(new FechaDisponible
                            {
                                Fecha = fechaString,
                                DesdeHora = desdeHoraString,
                                HastaHora = hastaHoraString
                            });
                        }
                    }
                }
            }
            return fechas;
        }

        public List<ImagenesPrueba> ObtenerImagenes(int idRegistroReciclaje)
        {
            var imagenes = new List<ImagenesPrueba>();
            string query = @"SELECT imagenes
                            FROM ImagenesPrueba
                            WHERE idRegistroReciclaje = @id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", idRegistroReciclaje);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imageBytes = (byte[])reader["imagenes"];

                            string base64String = Convert.ToBase64String(imageBytes);

                            string dataUrl = $"data:image/jpeg;base64,{base64String}";

                            imagenes.Add(new ImagenesPrueba
                            {
                                Imagenes = dataUrl
                            });
                        }
                    }
                }
            }
            return imagenes;
        }

        public bool InsertarAsignacion(int idRegistro, int idRecolector, int idReciclador, string fechaStr, string desdeHoraStr, string hastaHoraStr)
        {

            string query = @"INSERT INTO Asignacion (estado, idRegistroReciclaje, idRecolector, idReciclador, desdeHora, hastaHora, fecha)
                            VALUES (@estado, @idRegistroReciclaje, @idRecolector, @idReciclador,@desdeHora, @hastaHora, @fecha)";

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@estado", "Pendiente");
                        command.Parameters.AddWithValue("@idRegistroReciclaje", idRegistro);

                        command.Parameters.AddWithValue("@idRecolector", idRecolector);

                        command.Parameters.AddWithValue("@idReciclador", idReciclador);
                        command.Parameters.AddWithValue("@fecha", DateTime.Parse(fechaStr));
                        command.Parameters.AddWithValue("@desdeHora", TimeSpan.Parse(desdeHoraStr));
                        command.Parameters.AddWithValue("@hastaHora", TimeSpan.Parse(hastaHoraStr));


                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SQL Error: " + ex.Message);
                    return false;
                }
            }
        }

        public List<HistorialAsignacionViewModel> ObtenerHistorialAsignaciones(int idEmpresa)
        {
            var lista = new List<HistorialAsignacionViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
           SELECT a.idAsignacion,
       r.nombres + ' ' + r.apellidos AS NombreReciclador,
       c.nombre AS Categoria,
       r.imgPerfil AS ImgPerfil,
       r.idUsuario AS IdReciclador,
       a.estado AS Estado
FROM Asignacion a
INNER JOIN RegistroReciclaje rr ON a.idRegistroReciclaje = rr.idRegistroReciclaje
INNER JOIN Categoria c ON rr.idCategoria = c.idCategoria
INNER JOIN Reciclador r ON a.idReciclador = r.idUsuario
WHERE a.idRecolector = @IdEmpresa
ORDER BY a.fecha DESC


";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new HistorialAsignacionViewModel
                            {
                                IdAsignacion = Convert.ToInt32(reader["idAsignacion"]),
                                NombreReciclador = reader["NombreReciclador"].ToString(),
                                Categoria = reader["Categoria"].ToString(),
                                ImgPerfil = reader["ImgPerfil"] as byte[],
                                Estado = reader["Estado"].ToString(),
                                IdReciclador = Convert.ToInt32(reader["IdReciclador"])
                            });

                        }
                    }
                }
            }

            return lista;
        }


        public DetalleAsignacionViewModel ObtenerDetalleAsignacion(int idAsignacion)
        {
            DetalleAsignacionViewModel detalle = null;

            string query = @"
        SELECT 
            c.nombre AS Categoria,
            rr.descripcion,
            rr.fechaRegistro,
            CONCAT(r.nombres, ' ', r.apellidos) AS NombreReciclador,
            rr.idRegistroReciclaje
        FROM Asignacion a
        INNER JOIN RegistroReciclaje rr ON a.idRegistroReciclaje = rr.idRegistroReciclaje
        INNER JOIN Categoria c ON rr.idCategoria = c.idCategoria
        INNER JOIN Reciclador r ON a.idReciclador = r.idUsuario
        WHERE a.idAsignacion = @id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", idAsignacion);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idRegistro = reader.GetInt32(reader.GetOrdinal("idRegistroReciclaje"));

                            // Convertir fechas disponibles al tipo ViewModel
                            var fechasOriginales = ObtenerFechasDisponibles(idRegistro);
                            var fechasConvertidas = fechasOriginales.Select(f => new FechaDisponibleViewModel
                            {
                                Fecha = f.Fecha,
                                DesdeHora = f.DesdeHora,
                                HastaHora = f.HastaHora
                            }).ToList();

                            detalle = new DetalleAsignacionViewModel
                            {
                                Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                                Descripcion = reader.GetString(reader.GetOrdinal("descripcion")),
                                FechaRegistro = reader.GetDateTime(reader.GetOrdinal("fechaRegistro")),
                                NombreReciclador = reader.GetString(reader.GetOrdinal("NombreReciclador")),
                                FechasDisponibles = fechasConvertidas
                            };
                        }
                    }
                }
            }

            return detalle;
        }

        public List<EmpresaReporteDTO> ObtenerDatosEmpresasReporte(string mes)
        {
            var empresas = new List<EmpresaReporteDTO>();
            string query = @"
        SELECT 
            e.nombreEmpresa,
            u.correo AS correo,
            u.telefono AS telefono,
            u.fechaRegistro AS fechaRegistro,
            COUNT(a.idAsignacion) AS TotalAsignaciones,
            ISNULL(AVG(CAST(r.numEstrella AS FLOAT)), 0) AS PromedioEstrellas
        FROM EmpresaRecolector e
        INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
        LEFT JOIN Asignacion a ON e.idUsuario = a.idReciclador
        LEFT JOIN Reseña r ON e.idUsuario = r.idUsuario
        WHERE MONTH(a.fecha) = @Mes
        GROUP BY e.nombreEmpresa, u.correo, u.telefono, u.fechaRegistro;
    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Mes", Convert.ToInt32(mes));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            empresas.Add(new EmpresaReporteDTO
                            {
                                NombreEmpresa = reader["nombreEmpresa"].ToString(),
                                Correo = reader["correo"].ToString(),
                                Telefono = Convert.ToInt32(reader["telefono"]),
                                FechaRegistro = Convert.ToDateTime(reader["fechaRegistro"]),
                                TotalAsignaciones = Convert.ToInt32(reader["TotalAsignaciones"]),
                                PromedioEstrellas = Convert.ToDouble(reader["PromedioEstrellas"])
                            });
                        }
                    }
                }
            }

            return empresas;
        }


        public List<Reciclador> ObtenerRecicladoresFinalizadosPorEmpresa(int idEmpresa)
        {
            var lista = new List<Reciclador>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
            SELECT r.nombres, r.apellidos, u.correo
            FROM Asignacion a
            INNER JOIN Reciclador r ON a.idReciclador = r.idUsuario
            INNER JOIN Usuario u ON r.idUsuario = u.idUsuario
            WHERE a.idRecolector = @idEmpresa AND a.estado = 'Finalizado'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idEmpresa", idEmpresa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Reciclador
                            {
                                Nombres = reader.GetString(0),
                                Apellidos = reader.GetString(1),
                                Correo = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public int ObtenerTotalReciclajesFinalizados()
        {
            int total = 0;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
            SELECT COUNT(*) 
            FROM Asignacion a
            WHERE a.estado = 'Finalizado'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    total = (int)cmd.ExecuteScalar();
                }
            }

            return total;
        }

    }
}
