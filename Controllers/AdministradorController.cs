using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using Web_Recocycle.Data;
using Rotativa.AspNetCore;
using Web_Recocycle.Models;

namespace Web_Recocycle.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DatabaseHelper _helper;
        private readonly EmpresaHelper _empresaHelper;
        private readonly RecicladoresHelper _recicladoresHelper;
        private readonly DatabaseHelper1 _db;
        private readonly DatabaseHelper3 _db3;
        public AdministradorController(ApplicationDbContext context, IConfiguration configuration, DatabaseHelper1 db, DatabaseHelper3 db3)
        {
            _helper = new DatabaseHelper(configuration);
            _context = context;
            _configuration = configuration;
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            _empresaHelper = new EmpresaHelper(connectionString);
            _recicladoresHelper = new RecicladoresHelper(connectionString);
            _db = db;
            _db3 = db3;
        }

        [HttpPost]
        public JsonResult RegistrarAdministradorManual([FromBody] Administrador admin)
        {
            var correoSesion = HttpContext.Session.GetString("Correo");
            string correoSuperAdmin = "admin3@recocycle.com";

            if (string.IsNullOrEmpty(correoSesion) ||
                !correoSesion.Equals(correoSuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Solo el SuperAdmin puede crear administradores." });
            }

            admin.FechaRegistro = DateTime.Now;
            admin.FechaModificacion = DateTime.Now;
            admin.Estado = 1;
            admin.Rol = 1;

            int id = _db.RegistrarAdministrador(admin);
            return Json(new { success = true, idUsuario = id });
        }




        public IActionResult Index()
        {
            var correoSesion = HttpContext.Session.GetString("Correo");
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("InicioSesionUsuario", "Home");
            }


            var usuariosTop = _recicladoresHelper.ObtenerRankingRecicladores()
                .OrderByDescending(r => r.Puntos)
                .Take(3)
                .ToList();

            var premio = _context.Premio
                .OrderByDescending(p => p.FechaRegistro)
                .FirstOrDefault();


            var empresasActivas = _empresaHelper.ObtenerEmpresasActivas();
            var totalReciclajes = _db3.ObtenerTotalReciclajesFinalizados();


            ViewBag.TopUsuarios = usuariosTop;
            ViewBag.Premio = premio;
            ViewBag.EmpresasActivas = empresasActivas;
            ViewBag.TotalReciclajes = totalReciclajes;

            return View();
        }





        public IActionResult Ranking()
        {
            var recicladores = _recicladoresHelper.ObtenerRankingRecicladores();
            return View(recicladores);
        }

        //Premio

        public IActionResult Premio()
        {
            // Buscar si ya hay un premio activo cuya fecha límite aún no venció
            var premioActivo = _context.Premio
                .Where(p => p.FechaLimite >= DateTime.Now)
                .OrderByDescending(p => p.FechaRegistro)
                .FirstOrDefault();

            if (premioActivo != null)
            {
                TempData["MensajeConfirmacion"] =
                    $"Ya existe un premio activo hasta la fecha: {premioActivo.FechaLimite:dd/MM/yyyy}. ¿Desea registrar uno nuevo?";

                // guardamos la fecha para mostrarla en el Alert
                TempData["FechaLimite"] = premioActivo.FechaLimite.ToString("yyyy-MM-dd");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Premio(IFormFile ImgPremio, string Nombre, DateTime FechaLimite)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
                return RedirectToAction("LoginAdministrador");

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                ModelState.AddModelError("Nombre", "Debe ingresar un nombre válido para el premio.");
                return View();
            }

            if (ImgPremio == null || ImgPremio.Length == 0)
            {
                ModelState.AddModelError("ImgPremio", "Debe subir una imagen para el premio.");
                return View();
            }

            // 🔥 Eliminar premio activo si existe
            var premioActivo = _context.Premio
                .Where(p => p.FechaLimite >= DateTime.Now)
                .OrderByDescending(p => p.FechaRegistro)
                .FirstOrDefault();

            if (premioActivo != null)
            {
                _context.Premio.Remove(premioActivo);
                await _context.SaveChangesAsync();
            }

            // 🔄 Crear nuevo premio
            byte[] imagenBytes;
            using (var ms = new MemoryStream())
            {
                await ImgPremio.CopyToAsync(ms);
                imagenBytes = ms.ToArray();
            }

            var nuevoPremio = new Premio
            {
                Nombre = Nombre,
                FechaLimite = FechaLimite,
                ImgPremio = imagenBytes,
                FechaRegistro = DateTime.Now,
                IdUsuario = idUsuario.Value
            };

            _context.Premio.Add(nuevoPremio);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }









        //EMPRESA
        public IActionResult CrudEmpresa()
        {
            List<Empresa> empresas = _empresaHelper.ObtenerEmpresas();
            return View(empresas);
        }

        public IActionResult RegistrarEmpresa()
        {
            var categorias = _empresaHelper.ObtenerCategorias();
            ViewBag.Categorias = categorias;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarEmpresa(IFormFile logo, string nombreEmpresa, string[] categoria, string correo, string telefono, string contraseña)
        {
            if (logo == null || logo.Length == 0)
            {
                ViewBag.Error = "Debe subir un logo para registrar la empresa.";
                ViewBag.Categorias = _empresaHelper.ObtenerCategorias();
                return View();
            }
            byte[] logoBytes = null;

            if (logo != null && logo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await logo.CopyToAsync(ms);
                    logoBytes = ms.ToArray();
                }
            }
            else
            {
               
                var defaultLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "default-logo.png");

                if (System.IO.File.Exists(defaultLogoPath))
                {
                    logoBytes = await System.IO.File.ReadAllBytesAsync(defaultLogoPath);
                }
                else
                {
                    // Si no existe, puedes lanzar un error o dejar el logo como null
                    ViewBag.Error = "No se encontró el logo por defecto.";
                    return View();
                }
            }
            Empresa nuevaEmpresa = new Empresa
            {
                NombreEmpresa = nombreEmpresa,
                //Categoria = categoriasFinal,
                Correo = correo,
                Telefono = int.Parse(telefono),
                Contrasenia = contraseña,
                ImgLogo = logoBytes,
                FechaRegistro = DateTime.Now,
                FechaModificacion = DateTime.Now,
                Estado = 1,
                Rol = 2
            };

            int nuevoId = _empresaHelper.RegistrarUsuario(nuevaEmpresa);
            nuevaEmpresa.IdUsuario = nuevoId;

            bool registrado = _empresaHelper.RegistrarEmpresaRecolector(nuevaEmpresa);

            if (registrado)
            {
                foreach (var cat in categoria)
                {
                    int idCat = int.Parse(cat);
                    _empresaHelper.RegistrarEmpresaCategoria(nuevaEmpresa.IdUsuario, idCat);
                }

                return RedirectToAction("CrudEmpresa");
            }
            else
            {
                ViewBag.Error = "No se pudo registrar la empresa.";
                return View();
            }

        }


        [HttpPost]
        public IActionResult EliminarEmpresa(int idUsuario, int estado)
        {
            _empresaHelper.CambiarEstadoEmpresa(idUsuario, 0);
            return RedirectToAction("CrudEmpresa");
        }



        public IActionResult ModificarEmpresa(int id)
        {
            var empresa = _empresaHelper.ObtenerEmpresaPorId(id);
            if (empresa == null)
            {
                return NotFound();
            }

            ViewBag.Categorias = _empresaHelper.ObtenerCategorias();
            return View(empresa);
        }


        //BOTON MODIFICAR
        [HttpPost]
        public async Task<IActionResult> ModificarEmpresa(Empresa empresa, IFormFile Logo, int[] categoria)
        {
            if (Logo != null && Logo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Logo.CopyToAsync(ms);
                    empresa.ImgLogo = ms.ToArray();
                }
            }
            else
            {
                // Recuperar el logo actual si no se sube uno nuevo
                var empresaActual = _empresaHelper.ObtenerEmpresaPorId(empresa.IdUsuario);
                empresa.ImgLogo = empresaActual.ImgLogo;
            }

            empresa.CategoriasSeleccionadas = categoria.ToList();

            _empresaHelper.ActualizarEmpresa(empresa);

            return RedirectToAction("CrudEmpresa");
        }
        //BOTON DETALLE
        public IActionResult DetalleEmpresa(int id)
        {
            var empresa = _empresaHelper.ObtenerDatosBasicos(id);
            var clientes = _db3.ObtenerRecicladoresFinalizadosPorEmpresa(id);

            var modelo = new DetalleEmpresaViewModel
            {
                NombreEmpresa = empresa.NombreEmpresa,
                Correo = empresa.Correo,
                Estado = empresa.Estado,
                ImgLogo = empresa.ImgLogo,
                TotalClientes = clientes.Count,
                Clientes = clientes
            };

            return View(modelo);
        }




        [HttpGet]
        public IActionResult ResetearPuntos(int porcentaje)
        {
            if (porcentaje < 1 || porcentaje > 100)
            {
                // Podés redirigir con error o mostrar alerta
                TempData["Error"] = "Porcentaje inválido.";
                return RedirectToAction("Ranking");
            }

            _recicladoresHelper.ResetearPuntosPorPorcentaje(porcentaje);
            TempData["Mensaje"] = $"Se aplicó un reseteo del {porcentaje}% a todos los puntos.";
            return RedirectToAction("Ranking");
        }


        public IActionResult Reporte(string mes = null)
        {
            mes ??= DateTime.Now.Month.ToString();

            var recicladores = _db.ObtenerRecicladoresConDatos()
                .Select(r => new RecicladorReporteDTO
                {
                    Nombres = r.Nombres,
                    Apellidos = r.Apellidos,
                    Puntos = r.Puntos,                
                    PromedioEstrellas = r.PromedioEstrellas // idem
                })
                .ToList();

            var empresas = _db3.ObtenerDatosEmpresasReporte(mes);

            var modelo = new ReporteMensualViewModel
            {
                Recicladores = recicladores,
                Empresas = empresas
            };

            return View(modelo);
        }



        [HttpPost]
        public IActionResult DescargarReportePdf(string mes)
        {
            int mesInt = string.IsNullOrEmpty(mes) ? DateTime.Now.Month : int.Parse(mes);
            int año = DateTime.Now.Year;

            var reporteHelper = new ReporteHelper(_configuration.GetConnectionString("DefaultConnection"));

            var viewModel = new ReporteMensualViewModel
            {
                Recicladores = reporteHelper.ObtenerDatosRecicladoresReporte(mesInt, año),
                Empresas = reporteHelper.ObtenerDatosEmpresasReporte(mesInt, año),

                TotalUsuariosActivos = reporteHelper.ObtenerUsuariosActivos(mesInt, año),
                TotalReciclajesFinalizados = reporteHelper.ObtenerReciclajesFinalizados(mesInt, año),
                EmpresasActivas = reporteHelper.ObtenerEmpresasActivas(mesInt, año),
                Materiales = reporteHelper.ObtenerDistribucionMateriales(mesInt, año),
                Reseñas = reporteHelper.ObtenerDistribucionReseñas(mesInt, año),
                //Premio = _premioHelper.ObtenerPremioVigente()
            };


            return new Rotativa.AspNetCore.ViewAsPdf("ReportePdf", viewModel)
            {
                FileName = "ReporteMensual.pdf"
            };
        }




    }

}
