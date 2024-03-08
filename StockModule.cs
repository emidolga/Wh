using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class StockModule : NancyModule
    {
        public StockModule() : base("api/Stock/")
        {
            Get<Models.SalidaStock[]>("GetSalidaStockEntreFechas", p =>
            {
                this.RequiresAuthentication();
                List<Models.SalidaStock> salidaStockLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        salidaStockLista = HelperSQL.GetListaSalidaStockEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (salidaStockLista.ToArray());
            }, null, name: "Devuelve la lista de salidas de stock entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}