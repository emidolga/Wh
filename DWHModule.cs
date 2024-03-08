using Nancy;
using System.Collections.Generic;
using System;
using Nancy.Security;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.ReglasNegocio;
using Vemn.Framework.Logging;
using Vemn.Framework.ExceptionManagement;

namespace HostCaldenONNancy.Modules
{
    public class DWHModule : NancyModule
    {
        public DWHModule() : base("api/DWH/")
        {
            Get<Models.DWH.ArticuloFacturadoYRemitido[]>("GetArticulosFacturadosYRemitidos", p =>
            {
                this.RequiresAuthentication();
                List<Models.DWH.ArticuloFacturadoYRemitido> salidaLista = null;
                try
                {
                    DateTime desdeFecha, hastaFecha;
                    DateTime? fecha = Request.Query["fecha"];

                    if (fecha.HasValue)
                    {
                        desdeFecha = fecha.Value.Date;
                        hastaFecha = fecha.Value.Date.AddDays(1).AddMilliseconds(-1);
                    }
                    else
                    {
                        hastaFecha = DateTime.Now.Date.AddDays(1).AddMilliseconds(-1);
                        desdeFecha = DateTime.Now.Date.AddDays(-30);
                    }

                    salidaLista = DWH.HelperSQL.GetArticulosFacturadosYRemitidos(desdeFecha, hastaFecha);
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (salidaLista.ToArray());
            }, null, name: "Devuelve los elementos del DataWarehouse de ArticulosFacturadosYRemitidos. Si el parámetro {fecha} existe, retorna los elementos correspondientes a la fecha, y en caso de que no exista,  retorna los elementos de los últimos 30 días.");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}
