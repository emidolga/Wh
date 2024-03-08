using Nancy;
using Nancy.Security;
using System;
using System.Data;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Newtonsoft.Json;
using Vemn.Framework.Logging;
using Nancy.Responses;
using System.Net.Http;
using System.Text;
using Vemn.Framework.ExceptionManagement;

namespace HostCaldenONNancy.Modules
{
    public class CuentaContableModule : NancyModule
    {
        public CuentaContableModule() : base("api/CuentaContable/")
        {
            Get<Models.CuentaContable>("GetCuentaContable", p =>
            {
                this.RequiresAuthentication();
                Models.CuentaContable cuentaContable = null;
                try
                {
                    string codigoCuentaContable = Request.Query["codigoCuentaContable"];
                    cuentaContable = HelperSQL.GetCuentaContable(codigoCuentaContable);
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (cuentaContable);
            }, null, name: "Devuelve la cuenta contable dado el código. Parámetros: {codigoCuentaContable}");
        }
    }
}
