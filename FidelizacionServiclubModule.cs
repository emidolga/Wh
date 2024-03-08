using Aoniken.CaldenOil.Entidades.Modelos.Loyalty;
using Aoniken.CaldenOil.Entidades.Modelos.Loyalty.Serviclub.Dto;
using HostCaldenONNancy.Helpers;
using IntegracionFidelizacion.Factorias;
using IntegracionFidelizacion.Helpers;
using IntegracionFidelizacion.Modelos;
using IntegracionUtiles;
using Nancy;
using Newtonsoft.Json;
using System;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public sealed class FidelizacionServiclubModule : NancyModule
    {
        private readonly ILoyaltyGateway _loyaltyGateway = LoyaltyFactory.CrearClienteFidelizacion(FidelizacionProgramaEnum.Serviclub);

        public FidelizacionServiclubModule() : base("api/Serviclub/")
        {
            const int codigoError = 400;
            const string solicitudDeCancelacion = "Se solicito cancelar la operación";
            const string bodyNoPresente = "El cuerpo de la solicitud no puede ser nulo o vacío";

            //Por cuestiones de seguridad, Serviclub rechaza conexiones con protocolos de encriptación inferiores a TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            Post("Acumulacion", async (p, token) =>
            {
                FidelizacionSolicitudResultadoDto response;
                var solicitud = RequestHelper.RecuperarSolicitudRecibida<FidelizacionSolicitudServiclubDto>(Request);

                try
                {
                    if (token.IsCancellationRequested)
                    {
                        response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(null, codigoError, solicitudDeCancelacion);
                    }
                    else if (Request.Body is null)
                    {
                        response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(null, codigoError, bodyNoPresente);
                    }
                    else
                    {
                        response = await _loyaltyGateway.EnviarBloqueLoyalty(solicitud).ConfigureAwait(false);
                        if (! _loyaltyGateway.RegistrarSolicitud(response, out string errorMsg))
                            LogUtils.LogError(errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                    HacerLogError(ex);
                }

                return JsonConvert.SerializeObject(response);
            }, null, "Acumulación de puntos Serviclub");

            Post("Acumulacion/Desistir", async (p, token) =>
            {
                FidelizacionSolicitudResultadoDto response;
                var solicitud = RequestHelper.RecuperarSolicitudRecibida<FidelizacionSolicitudServiclubDto>(Request);

                try
                {
                    if (token.IsCancellationRequested)
                    {
                        response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, codigoError, solicitudDeCancelacion);
                    }
                    else if (Request.Body is null)
                    {
                        response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, codigoError, bodyNoPresente);
                    }
                    else
                    {
                        response = await _loyaltyGateway.EnviarBloqueLoyalty(solicitud).ConfigureAwait(false);
                        if (!_loyaltyGateway.RegistrarSolicitud(response, out string errorMsg))
                            LogUtils.LogError(errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                    HacerLogError(ex);
                }

                return JsonConvert.SerializeObject(response);
            }, null, "Desistir la acumulación de puntos Serviclub anterior");
        }

        private static void HacerLogError(Exception ex)
        {
            if (ex != null)
                Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
        }
    }
}