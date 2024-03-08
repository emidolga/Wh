using Nancy;
using System.Collections.Generic;
using System;
using Nancy.Security;
using Vemn.Framework.Logging;
using Vemn.Framework.ExceptionManagement;

namespace HostCaldenONNancy.Modules
{
    public class CajasModule : NancyModule
    {
        public CajasModule() : base("api/Cajas/")
        {
            Get<Models.Caja[]>("GetAllCajas", p =>
            {
                this.RequiresAuthentication();
                List<Models.Caja> cajasLista = HelperSQL.GetListaCajas();
                return (cajasLista.ToArray());
            }, null, name: "Retorna una lista todas las cajas de todas las estaciones.");

            Get<Models.Caja>("GetCaja", p =>
            {
                this.RequiresAuthentication();
                Models.Caja caja = null;
                try
                {
                    int? idCaja = Request.Query["idCaja"];
                    if (idCaja != null)
                    {
                        caja = HelperSQL.GetCajaPorId(idCaja.Value);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (caja);
            }, null, name: "Devuelve los detalles de una caja , dado su identificador. Parámetros: {idCaja}");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }

    public class CierresModule : NancyModule
    {
        public CierresModule() : base("api/Cierres/")
        {
            Get<DateTime[]>("GetUltimosCierresTurno", p =>
            {
                this.RequiresAuthentication();
                List<DateTime> listaUltimosCierres = new List<DateTime>();
                try
                {
                    int? idEstacion = this.Request.Query["idEstacion"];
                    int? idCaja = this.Request.Query["idCaja"];
                    DateTime? fecha = this.Request.Query["fecha"];
                    Logger.Default.DebugFormat("GET: GetUltimosCierresTurno");
                    Logger.Default.DebugFormat("idEstacion: {0}", idEstacion);
                    Logger.Default.DebugFormat("idCaja: {0}", idCaja);
                    Logger.Default.DebugFormat("fecha: {0}", fecha);
                    if (idEstacion != null & idCaja != null && fecha != null)
                    {                        
                        listaUltimosCierres = HelperSQL.GetUltimosCierresTurno(idEstacion.Value, idCaja.Value, fecha.Value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (listaUltimosCierres.ToArray());
            }, null, name: "Retorna el conjunto de cierres de turno asociados a la estación, la caja y fecha dadas. Parámetros: {idEstacion} {idCaja} {fecha}");

            Get<Models.InformacionCierreTurno>("GetInformacionCierreTurno", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetInformacionCierreTurno", null);
                this.RequiresAuthentication();
                Models.InformacionCierreTurno cierre = null;
                try
                {
                    int? idEstacion = this.Request.Query["idEstacion"];
                    int? idCaja = this.Request.Query["idCaja"];
                    DateTime? fechaHoraCierre = null;
                    string fechaCadena = this.Request.Query["fechaHoraCierre"];
                    if (fechaCadena.Length > 19)
                    {
                        fechaHoraCierre = Convert.ToDateTime(fechaCadena.Substring(0, 19));
                    }
                    else
                    {
                        fechaHoraCierre = Convert.ToDateTime(fechaCadena);
                    }
                    Logger.Default.DebugFormat("GET: GetInformacionCierreTurno");
                    Logger.Default.DebugFormat("idEstacion: {0}", idEstacion);
                    Logger.Default.DebugFormat("idCaja: {0}", idCaja);
                    Logger.Default.DebugFormat("fechaHoraCierre: {0}", fechaHoraCierre);
                    //if (idEstacion != null & idCaja != null && fechaHoraCierre != null)
                    if (idEstacion.HasValue & idCaja.HasValue && fechaHoraCierre.HasValue)
                    {
                        cierre = HelperSQL.GetInformacionCierreTurno(idEstacion.Value, idCaja.Value, fechaHoraCierre.Value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (cierre);
            }, null, name: "Retorna el detalle de un cierre de turno asociado a la estación, la caja y la fecha y hora exactas del cierre. Parámetros: {idEstacion} {idCaja} {fechaHoraCierre}");
        }
    }
}