using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vemn.Framework.Logging;
using Aoniken.CaldenOil.Helpers;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.ReglasNegocio;

namespace HostCaldenONNancy.Modules
{
    public class DepositoModule : NancyModule
    {
        
        public DepositoModule() : base("api/Depositos/")
        {
            Get<Models.Deposito[]>("GetAllDepositos", p =>
            {
                this.RequiresAuthentication();
                List<Models.Deposito> depositosLista = HelperSQL.GetListaDepositos();
                return (depositosLista.ToArray());
            }, null, name: "Devuelve la lista de todos los depósitos.");

            Get<Models.Deposito[]>("GetDepositosEmpleado", p =>
            {
                this.RequiresAuthentication();
                int idEmpleado = this.Request.Query["idEmpleado"];
                List<Models.Deposito> depositosLista = HelperSQL.GetListaDepositosEmpleado(idEmpleado);
                return (depositosLista.ToArray());
            }, null, name: "Dado un empleado, devuelve la lista de depósitos asociados a él. Parámetros: {idEmpleado}");
        }
    }
}