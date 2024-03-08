using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class ChequesTercerosModule : NancyModule
    {
        public ChequesTercerosModule() : base("api/ChequesTerceros/")
        {
            Get<Models.ChequeTercero[]>("GetAllChequesTerceros", p =>
            {
                this.RequiresAuthentication();
                List<Models.ChequeTercero> chequesTercerosLista = HelperSQL.GetListaChequesTerceros();
                return (chequesTercerosLista.ToArray());
            }, null, name: "Devuelve la lista de todos los cheques de terceros.");

            Get<Models.ChequeTercero>("GetChequeTercero", p =>
            {
                this.RequiresAuthentication();
                Models.ChequeTercero chequeTercero = null;
                try
                {
                    int? idChequeTercero = Request.Query["idChequeTercero"];
                    if (idChequeTercero != null)
                    {
                        chequeTercero = HelperSQL.GetChequeTerceroPorId(idChequeTercero.Value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (chequeTercero);
            }, null, name: "Devuelve el cheque de tercero, dado su identificador. Parámetros: {idChequeTercero}");

            Get<Models.ChequeTercero[]>("GetChequesTercerosEntreFechas", p =>
            {
                this.RequiresAuthentication();
                List<Models.ChequeTercero> chequesTercerosLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        chequesTercerosLista = HelperSQL.GetListaChequesTercerosEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (chequesTercerosLista.ToArray());
            }, null, name: "Devuelve la lista cheques de terceros entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Models.ChequeTercero[]>("GetEgresoChequesTercerosEntreFechas", p =>
            {
                this.RequiresAuthentication();
                List<Models.ChequeTercero> chequesTercerosLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        chequesTercerosLista = HelperSQL.GetListaEgresoChequesTercerosEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (chequesTercerosLista.ToArray());
            }, null, name: "Devuelve la lista cheques de terceros entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}