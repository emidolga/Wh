using Nancy;
using Nancy.Security;
using System.Collections.Generic;
using Vemn.Framework;

namespace HostCaldenONNancy.Modules
{
    public class EstacionesModule : NancyModule
    {
        public EstacionesModule() : base("api/Estaciones/")
        {
            Get<Models.Estacion[]>("GetAllEstaciones", p =>
            {
                this.RequiresAuthentication();
                List<Models.Estacion> estacionesLista = HelperSQL.GetListaEstaciones();
                return (estacionesLista.ToArray());
            }, null, name: "Devuelve la lista de todas las estaciones.");

            Get<Models.EstacionCompleta>("GetEstacionLocal", p =>
            {
                this.RequiresAuthentication();
                Models.EstacionCompleta result = HelperSQL.GetEstacionLocal();
                return (result);
            }, null, name: "Devuelve la estación considerada como local");

            Get<Models.EstacionAzure>("GetIdEstacionAzure", p =>
             {
                 int idEstacionAzure = ConfigurationReader.GetKeyValue("IdEstacionAzure", 0);

                 return new Models.EstacionAzure() { IdEstacionAzure = idEstacionAzure };

             }, null, name: "Uso interno");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}