using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class DespachosModule : NancyModule
    {
        public DespachosModule() : base("api/Despachos/")
        {
            Get<Models.InformacionDespacho[]>("GetDespachosInformacion", p =>
            {
                try
                {
                    this.RequiresAuthentication();

                    DateTime desdeFecha = this.Request.Query["desdeFecha"];
                    DateTime hastaFecha = this.Request.Query["hastaFecha"];

                    List<Models.InformacionDespacho> despachosLista = HelperSQL.GetInformacionDespachos(desdeFecha, hastaFecha);

                    return (despachosLista.ToArray());
                }
                catch (Exception ex)
                {
                    Logger.Default.ErrorFormat("Error en GetDespachosInformacion: {0}", ex.Message);
                    return null;
                }
            }, null, name: "Recuperar información detallada de los despachos en un intervalo de tiempo");
        }
    }
}
