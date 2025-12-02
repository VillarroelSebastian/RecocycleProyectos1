using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Web_Recocycle.Data;
using Web_Recocycle.Models;
using Microsoft.Data.SqlClient;

namespace Web_Recocycle.Controllers
{
    
    public class EmpresaController : Controller
    {
        private readonly DatabaseHelper3 _db3;
        private readonly DatabaseHelper1 _db;

        public EmpresaController(DatabaseHelper3 db3, DatabaseHelper1 db1)
        {
            _db3 = db3;
            _db = db1;
        }


        public IActionResult Index()
        {
            List<RegistrosReciclaje> registros = _db3.ObtenerTodosLosRegistrosReciclaje();

            return View(registros);
        }

        [HttpPost]
        public IActionResult AsignarRecoleccion([FromBody] Asignacion model)
        {
            try
            {
                bool resultado = _db3.InsertarAsignacion(
                    model.IdRegistroReciclaje,
                    model.IdRecolector,
                    model.IdReciclador,
                    model.Fecha,
                    model.DesdeHora,
                    model.HastaHora
                );

                if (resultado)
                {
                    return Json(new { success = true, message = "Recolección asignada correctamente." });
                }
                else
                {
                    return Json(new { success = false, message = "Error al insertar la asignación en la base de datos." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }
        public IActionResult HistorialRecolecta()
        {
            if (SesionGlobal.IdUsuario == 0)
            {
                return RedirectToAction("InicioSesionUsuario", "Home");
            }

            int idEmpresa = SesionGlobal.IdUsuario;


            List<HistorialAsignacionViewModel> historial = _db3.ObtenerHistorialAsignaciones(idEmpresa);

            return View(historial);
        }


        [HttpGet]
        public JsonResult ObtenerDetalleAsignacion(int id)
        {
            var asignacion = _db3.ObtenerDetalleAsignacion(id);


            if (asignacion == null)
            {
                return Json(new { error = true, mensaje = "No se encontró la asignación." });

            }

            return Json(asignacion);

        }

        //[httppost]
        //public jsonresult guardarreseñareciclador([frombody] reseñarequest req)
        //{
        //    bool reseñaok = _db.guardarreseña(req.idusuario, req.numestrella);
        //    bool puntosok = _db.sumarpuntosalreciclador(req.idusuario, req.numestrella);

        //    return json(new { success = reseñaok && puntosok });
        //}
        [HttpPost] public JsonResult GuardarReseñaReciclador([FromBody] ReseñaRequest req) 
        {
            bool reseñaOk = _db.GuardarReseña(req.idUsuario, req.numEstrella);
            bool puntosOk = _db.SumarPuntosAlReciclador(req.idUsuario, req.numEstrella);
            return Json(new { success = reseñaOk && puntosOk });
        }



    }
}
