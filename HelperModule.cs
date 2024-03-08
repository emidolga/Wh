using Nancy;
using Nancy.Security;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class HelperModule : NancyModule
    {
        public HelperModule() : base("api/Helper/")
        {
            Get<Models.Info[]>("GetInfo", p =>
            {
                this.RequiresAuthentication();
                
                var path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Aoniken\CaldenOil.Net\Release";
                var assembly = Assembly.LoadFile(System.IO.Path.Combine(path, "CaldenOil.exe"));

                Models.Info info = new Models.Info
                {
                    VersionWebHost = WebServerCaldenONNancy.VersionWebhost,
                    VersionCaldenOil = (assembly?.GetName().Version ?? new Version(99, 99, 99, 99)).ToString()
                };

                List<Models.Info> lista = new List<Models.Info>
                {
                    info
                };
                return (lista.ToArray());
            }, null, name: "Devuelve la versión de CaldenOil instalada.");


            Post<Response>("SendNotification", p =>
            {
                this.RequiresAuthentication();

                HttpStatusCode httpStatusCode = HttpStatusCode.OK;

                try
                {
                    string parametroIdCliente = this.Request.Form["IdCliente"];

                    int.TryParse(parametroIdCliente, out int idCliente);

                    bool enviaResumenPorMail = HelperSQL.GetDetalleCliente_EnviaResumenPorMail(idCliente);

                    if (enviaResumenPorMail)
                    {
                        SendGrid.SendGridClientOptions options = new SendGrid.SendGridClientOptions();

                        options.ApiKey =   //   PAD

                        SendGrid.SendGridClient client = new SendGrid.SendGridClient(options);

                        string subject = this.Request.Form["Subject"];
                        string htmlContent = this.Request.Form["HtmlContent"];
                        string to = this.Request.Form["To"];
                        string filename = this.Request.Form["Filename"];

                        var msg = new SendGridMessage()
                        {
                            From = new EmailAddress("noreply@ServicioNotificacionAoniken.com", "Notificaciones Aoniken"),
                            Subject = subject,
                            HtmlContent = htmlContent
                        };
                        msg.AddTo(new EmailAddress(to, to));

                        foreach (var adjunto in this.Request.Files)
                        {
                            byte[] bytesAdjunto = new byte[adjunto.Value.Length];

                            adjunto.Value.Read(bytesAdjunto, 0, Convert.ToInt32(adjunto.Value.Length));

                            string base64Content = Convert.ToBase64String(bytesAdjunto);

                            msg.AddAttachment(filename, base64Content);
                        }

                        SendGrid.Response response = client.SendEmailAsync(msg).Result;

                        if (!response.IsSuccessStatusCode)
                        {
                            httpStatusCode = HttpStatusCode.BadRequest;
                        }
                    }
                    else
                    {
                        //  si el cliente tiene "EnviaResumenPorMail=FALSE"...

                        httpStatusCode = HttpStatusCode.NotAcceptable;  //  HTTP 406
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    httpStatusCode = HttpStatusCode.InternalServerError;
                }

                return (new Response() { StatusCode = httpStatusCode});

            }, null, name: "(uso interno)");
        }
    }
}