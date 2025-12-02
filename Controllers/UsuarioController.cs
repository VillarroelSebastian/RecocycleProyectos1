using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Web_Recocycle.Data;
using Web_Recocycle.Helpers;
using Web_Recocycle.Models;

namespace Web_Recocycle.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ILogger<UsuarioController> _logger;
        private readonly DatabaseHelper1 _db;
        private readonly DatabaseHelper2 _db2;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context; // <-- agregado

        public UsuarioController(
            ILogger<UsuarioController> logger,
            DatabaseHelper1 db,
            IWebHostEnvironment env,
            DatabaseHelper2 db2,
            ApplicationDbContext context)   // <-- agregado
        {
            _logger = logger;
            _db = db;
            _env = env;
            _db2 = db2;
            _context = context; // <-- asignado
        }

        public ActionResult PerfilReciclador()
        {
            int id = SesionGlobal.IdUsuario;

            // Obtenemos los datos del usuario
            var vm = _db.DatosUsuario(id); // UsuarioRecicladorViewModel

            // Premio más reciente activo
            var premio = _context.Premio
                .Where(p => p.FechaLimite >= DateTime.Today)
                .OrderByDescending(p => p.FechaRegistro)
                .FirstOrDefault();

            vm.Premio = premio; // asignamos el premio al ViewModel

            return View(vm);
        }

        public ActionResult ModificarDatos()
        {
            int id = SesionGlobal.IdUsuario;
            var vm = _db.ObtenerDatos(id);
            return View(vm);
        }

        public ActionResult Publicaciones()
        {
            int currentUserId = SesionGlobal.IdUsuario;
            List<RegistrosReciclaje> userItems = _db2.GetUserRecyclingItems(currentUserId);
            return View(userItems);
        }

        [HttpPost]
        public ActionResult Eliminar(int id)
        {
            int rowsAffected = _db2.EliminarRegistroReciclaje(id);
            if (rowsAffected > 0)
            {
                // Retorna JSON de éxito
                return Json(new { success = true, message = "Se eliminó correctamente la publicación." });
            }
            else
            {
                // Retorna JSON de error
                return Json(new { success = false, message = "No se pudo eliminar la publicación. Es posible que ya haya sido eliminada." });
            }
        }

        public ActionResult RegistroReciclaje()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegistrarPublicacion(RegistrosReciclaje registro, List<FechaDisponible> FechasDisponibles, List<ImagenesPrueba> Imagenes)
        {
            try
            {
                Console.WriteLine($"Normal Coordenadas - Lat: {registro.Latitud}, Lng: {registro.Longitud}");

                if (registro != null)
                {
                    registro.Latitud = registro.Latitud;
                    registro.Longitud = registro.Longitud;
                    Console.WriteLine($"Controller Coordenadas - Lat: {registro.Latitud}, Lng: {registro.Longitud}");
                }

                if (registro == null || registro.Categoria == null || string.IsNullOrEmpty(registro.Descripcion) || registro.Latitud == " " || registro.Longitud == " " || FechasDisponibles == null || FechasDisponibles.Count == 0)
                {
                    TempData["Registrado"] = 0;
                    return RedirectToAction("RegistroReciclaje", "Usuario");
                }

                registro.FechasDisponibles = FechasDisponibles;
                registro.Imagenes = Imagenes;

                int resultado = _db2.GuardarRegistroCompleto(registro);
                TempData["Registrado"] = resultado;

                if (resultado == 1)
                {
                    return RedirectToAction("RegistroReciclaje", "Usuario");
                }
                else
                {
                    return RedirectToAction("RegistroReciclaje", "Usuario");
                }
            }
            catch (Exception ex)
            {
                TempData["Registrado"] = 0;
                return RedirectToAction("RegistroReciclaje", "Usuario");
            }
        }

        //Notiificaciones;
        [HttpGet]
        public JsonResult ObtenerEmpresasInteresadas()
        {
            int idReciclador = SesionGlobal.IdUsuario;
            var empresas = _db.ObtenerEmpresasInteresadas(idReciclador);
            return Json(empresas);
        }

        [HttpPost]
        public JsonResult ConfirmarAsignacion([FromBody] ConfirmarAsignacionRequest req)
        {
            bool ok = _db.ActualizarEstadoAsignacion(req.IdAsignacion, "Confirmado");
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult RechazarAsignacion([FromBody] ConfirmarAsignacionRequest req)
        {
            bool ok = _db.ActualizarEstadoAsignacion(req.IdAsignacion, "Rechazada");
            return Json(new { success = ok });
        }

        [HttpGet]
        public JsonResult ObtenerNotificaciones()
        {
            int idReciclador = SesionGlobal.IdUsuario;
            var pendientes = _db.ObtenerEmpresasPorEstado(idReciclador, "Pendiente");
            var confirmadas = _db.ObtenerEmpresasPorEstado(idReciclador, "Confirmado");

            return Json(new { pendientes, confirmadas });
        }

        [HttpPost]
        public JsonResult FinalizarAsignacion([FromBody] ConfirmarAsignacionRequest req)
        {
            bool ok = _db.ActualizarEstadoAsignacion(req.IdAsignacion, "Finalizado");
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult GuardarReseña([FromBody] ReseñaRequest req)
        {
            bool ok = _db.GuardarReseña(req.idUsuario, req.numEstrella);
            return Json(new { success = ok });
        }

        [HttpPost]
        public IActionResult ModificacionDatosUsuario(Usuario usuario, Reciclador reciclador)
        {
            int idUsuarioLoggeado = SesionGlobal.IdUsuario;

            bool exito = _db.ModificarDatosUsuario(idUsuarioLoggeado, usuario, reciclador);

            if (exito)
            {
                return RedirectToAction("PerfilReciclador", "Usuario");
            }
            else
            {
                ModelState.AddModelError("", "Error al actualizar los datos o la imagen. La operación fue revertida.");
                return View(new { Usuario = usuario, Reciclador = reciclador });
            }
        }

        [HttpPost]
        public JsonResult CambiarContrasena([FromBody] CambiarContrasenaRequest req)
        {
            int idUsuario = SesionGlobal.IdUsuario;

            // 1. Obtener contraseña actual del usuario
            string hashActual = _db.ObtenerHashContrasenia(idUsuario);
            if (HashHelper.HashPassword(req.current) != hashActual)
            {
                return Json(new { success = false, mensaje = "La contraseña actual es incorrecta." });
            }

            // 2. Actualizar a la nueva contraseña
            string nuevaHash = HashHelper.HashPassword(req.newPass);
            bool exito = _db.CambiarContrasenia(idUsuario, nuevaHash);

            return Json(new { success = exito });
        }

        public class CambiarContrasenaRequest
        {
            public string current { get; set; }
            public string newPass { get; set; }
        }
    }
}
