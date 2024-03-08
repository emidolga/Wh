using Jose;
using Microsoft.Azure.NotificationHubs;
using Nancy;
using Nancy.Security;
using System;
using System.Data;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Aoniken.CaldenOil.Common;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HostCaldenONNancy.Modules
{
    public class PatentesModule : NancyModule
    {
        
        public PatentesModule() : base("api/Patentes/")
        {

            //Get<Models.Pedido[]>("GetPedidos", p =>
            //{
            //    this.RequiresAuthentication();
            //    int idEmpleado = this.Request.Query["idEmpleado"];
            //    List<Models.Pedido> listaPedidos = HelperSQL.GetListaPedidos(idEmpleado);

            //    return (listaPedidos.ToArray());

            //});

            Post<Models.RespuestaEnviarPatente>("enviarPatente", p =>
            {
                try
                {
                    this.RequiresAuthentication();

                    string foto = this.Request.Query["foto"];
                    string patente = this.Request.Query["patente"];
                    int posicion = this.Request.Query["posicion"];

                    Models.RespuestaEnviarPatente respuesta = new Models.RespuestaEnviarPatente
                    {
                        Success = true,
                        Message = "Cliente vinculado",
                        Result = new Models.ResultRespuestaEnviarPatente()
                        {
                            Codigo = "",
                            Nombre = "",
                            FormaDePago = "",
                            MaximoEnCuentaCorriente = 0,
                            Consumos = 0,
                            Permitido = 0,
                            Ultimo_Combustible_Cargado = "",
                            Fecha_Ultima_Carga = DateTime.Now
                        }
                    };

                    return respuesta;
                   
                }
                catch (Exception ex)
                {
                    string mensajeError = ex.Message.ToString();

                    if (ex is Nancy.ErrorHandling.RouteExecutionEarlyExitException)
                    {
                        if (!string.IsNullOrWhiteSpace(((Nancy.ErrorHandling.RouteExecutionEarlyExitException)ex).Reason))
                        {
                            mensajeError = ((Nancy.ErrorHandling.RouteExecutionEarlyExitException)ex).Reason;
                        }
                    }

                    Models.RespuestaEnviarPatente respuesta = new Models.RespuestaEnviarPatente
                    {
                        Success = false,
                        Message = mensajeError
                    };
                    return respuesta;
                }
            }, null, name: "(uso interno)");
        }
    }
}