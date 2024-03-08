using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class ChequesPropiosModule : NancyModule
    {
        public ChequesPropiosModule() : base("api/ChequesPropios/")
        {
            Get<Models.ChequePropio[]>("GetEgresoChequesPropiosEntreFechas", p =>
            {
                this.RequiresAuthentication();
                List<Models.ChequePropio> chequesPropiosLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        chequesPropiosLista = HelperSQL.GetListaEgresoChequesPropiosEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (chequesPropiosLista.ToArray());
            }, null, name: "Devuelve la lista cheques propios entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}