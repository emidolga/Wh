using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class TanquesModule : NancyModule
    {
        public TanquesModule() : base("api/Tanques/")
        {
            Get<Models.Tanque[]>("GetAllTanques", p =>
            {
                this.RequiresAuthentication();
                IndexModule.LoguearRequest(this.Request);
                IndexModule.LoguearRequestQuery(this.Request.Query);
                List<Models.Tanque> tanquesLista = HelperSQL.GetListaTanques();
                return (tanquesLista.ToArray());
            },null, name:"Recuperar todos los tanques");

            Get<Models.Tanque[]>("GetTanquesPorEstacion", p =>
            {
                this.RequiresAuthentication();
                IndexModule.LoguearRequest(this.Request);
                IndexModule.LoguearRequestQuery(this.Request.Query);
                List<Models.Tanque> tanquesLista = new List<Models.Tanque>();
                try
                {
                    int? idEstacion = this.Request.Query["idEstacion"];
                    if (idEstacion != null)
                    {
                        tanquesLista = HelperSQL.GetListaTanques();
                        tanquesLista = tanquesLista.FindAll(t => t.Estacion.IdEstacion.Equals(idEstacion));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
                }
                return (tanquesLista.ToArray());
            }, null, name: "Recuperar todos los tanques de una estación. Parámetro: {idEstacion}");

            Get<Models.InformacionTanque>("GetInformacionActualTanque", p =>
            {
                this.RequiresAuthentication();
                IndexModule.LoguearRequest(this.Request);
                IndexModule.LoguearRequestQuery(this.Request.Query);
                Models.InformacionTanque info = null;
                try
                {
                    int? idTanque = this.Request.Query["idTanque"];
                    if (idTanque != null)
                    {
                        info = HelperSQL.GetInformacionTanque(idTanque.Value, System.DateTime.Now);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
                }
                return (info);
            }, null, name: "Recuperar la información de un tanque. Parámetro: {idTanque}");

            Get<Models.InformacionTanque>("GetInformacionHistoricaTanque", p =>
            {
                this.RequiresAuthentication();
                IndexModule.LoguearRequest(this.Request);
                IndexModule.LoguearRequestQuery(this.Request.Query);
                Models.InformacionTanque info = null;
                try
                {
                    int? idTanque = this.Request.Query["idTanque"];
                    System.DateTime? fecha = this.Request.Query["fecha"];
                    if (idTanque != null && fecha != null)
                    {
                        info = HelperSQL.GetInformacionTanque(idTanque.Value, fecha.Value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
                }
                return (info);
            }, null, name: "Recuperar la información histórica de un tanque. Parámetros: {idTanque} {fecha}");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }        
    }
}