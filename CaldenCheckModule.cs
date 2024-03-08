using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vemn.Framework.Logging;
using Aoniken.CaldenOil.Helpers;
using Aoniken.CaldenOil.Entidades;

namespace HostCaldenONNancy.Modules
{
    public class CaldenCheckModule : NancyModule
    {
        public CaldenCheckModule() : base("api/CaldenCheck/")
        {
            

            Get<Models.Bono>("GetBono", p =>
            {
                string codigoBarra = this.Request.Query["codigoBarra"];
                Bono bono = null;

                if (codigoBarra != null)
                {
                    bono = HelperPartida.RecuperarDocumentoPorCodigoBarra(codigoBarra, Bono.TiposBono.ValePropio);
                }

                if (bono == null)
                {
                    return null;
                }
                else
                {
                    Models.Bono bonoRecuperado = new Models.Bono
                    {
                        Estado = bono.Estado.ToString(),
                        Descripcion = bono.Descripcion,
                        FechaVencimiento = bono.FechaVencimiento,
                        Importe = bono.ImporteVale
                    };

                    return bonoRecuperado;
                }
            }, null, name: "Dado un código de barras, retorna el vale propio (CaldenCheck) asociado y su estado. Parámetros: {codigoBarra}");
        }
    }
}