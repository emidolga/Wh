using Jose;
using Microsoft.Azure.NotificationHubs;
using Nancy;
using System;
using System.Collections.Generic;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;
using NPoco;

namespace HostCaldenONNancy.Modules
{
    public class UsuariosModule : NancyModule
    {
        public UsuariosModule() : base("api/Usuarios/")
        {
            //After.AddItemToEndOfPipeline((ctx) =>
            //{
            //    ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
            //        .WithHeader("Access-Control-Allow-Methods", "POST,GET")
            //        .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
            //});

            Get<string>("IsUsuarioValido", p =>
            {
                
                Models.Usuario usuario = null;
                Models.AuthToken authToken = null;
                string token = String.Empty;
                string nombre = this.Request.Query["nombre"];
                string password = this.Request.Query["password"];
                if (!String.IsNullOrEmpty(nombre) && !String.IsNullOrEmpty(password))
                {
                    usuario = HelperSQL.ValidarUsuario(nombre, password);
                }
                if (usuario != null)
                {
                    authToken = new Models.AuthToken() { UserId = usuario.SyncGUID, UserLogin = usuario.Nombre, UserName = usuario.Nombre, ExpirationDateTime = System.DateTime.Now.AddDays(365) };
                    object key = new Models.AuthSettings().SecretKey;
                    token = Jose.JWT.Encode(authToken, key, JwsAlgorithm.HS256);
                }
                return (token);
            }, null, name: "(deprecado) Valida un usuario dado su nombre y password. Si es válido, la respuesta incluye el Token para autenticar el resto de las operaciones.  Parámetros: {nombre} {password}");

            Get<Models.Empleado>("IsUsuarioValidoNuevaVersion", p =>
            {
                Models.Usuario usuario = null;
                Models.AuthToken authToken = null;
                string token = string.Empty;
                string nombre = this.Request.Query["nombre"];
                string password = this.Request.Query["password"];
                if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(password))
                {
                    usuario = HelperSQL.ValidarUsuario(nombre, password);
                }
                if (usuario != null)
                {
                    authToken = new Models.AuthToken()
                    {
                        UserId = usuario.SyncGUID,
                        UserLogin = usuario.Nombre,
                        UserName = usuario.Nombre,
                        ExpirationDateTime = DateTime.Now.AddDays(365)
                    };
                    object key = new Models.AuthSettings().SecretKey;
                    token = Jose.JWT.Encode(authToken, key, JwsAlgorithm.HS256);

                    Models.Empleado response = new Models.Empleado()
                    {
                        IdEmpleado = usuario.IdUsuario,
                        Token = token,
                        CaldenON = usuario.CaldenON,
                        IMAMobile = usuario.IMAMobile,
                        Salesman_PermiteModificarPrecio = usuario.Salesman_PermiteModificarPrecio,
                        IdGrupoVendedores = usuario.IdGrupoVendedores
                    };

                    return response;
                }
                else
                {
                    return null;
                }
                //turn (token);
            }, null, name: "Valida un usuario dado su nombre y password. Si es válido, la respuesta incluye los datos del empleado asociado y el Token para autenticar el resto de las operaciones.  Parámetros: {nombre} {password}");

            Get<Response>("", p =>
            {
                return (new Response() { StatusCode = HttpStatusCode.BadRequest });
            });

            base.Put("NotificarDominioUtilizado", p =>
            {
                bool result = true;
                try
                {
                    string hashDominio = this.Request.Query["hashDominio"];
                    string registrationID = this.Request.Query["registrationId"];
                    string azureRegId = this.Request.Query["azureRegId"];
                    Logger.Default.DebugFormat("PUT: NotificarDominioUtilizado");

                    if (string.IsNullOrWhiteSpace(registrationID) || string.IsNullOrWhiteSpace(azureRegId) || string.IsNullOrWhiteSpace(hashDominio))
                    {
                        result = false;
                    }
                    else
                    {
                        HelperSQL.PersistirHashDominio(hashDominio);
                        RegistrationDescription registration = WebServerCaldenONNancy.NotificationHubClient.GetRegistrationAsync<RegistrationDescription>(registrationID).Result;
                        if (registration != null)
                        {
                            if (registration.Tags == null)
                            {
                                registration.Tags = new HashSet<string>();
                            }
                            if (!registration.Tags.Contains(hashDominio))
                            {
                                registration.Tags.Add(hashDominio);
                                RegistrationDescription resultado = WebServerCaldenONNancy.NotificationHubClient.UpdateRegistrationAsync(registration).Result;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    result = false;
                }
                return (result);
            }, null, name: "(uso interno)");
        }
    }
}