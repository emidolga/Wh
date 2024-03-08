using Aoniken.CaldenOil.Entidades.Modelos.Loyalty;
using Aoniken.CaldenOil.Entidades.Modelos.Loyalty.Thalamus.Dto;
using HostCaldenONNancy.Helpers;
using IntegracionFidelizacion.Factorias;
using IntegracionFidelizacion.Helpers;
using IntegracionFidelizacion.Modelos;
using Nancy;
using Newtonsoft.Json;
using System;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public sealed class FidelizacionModule : NancyModule
    {
        private readonly ILoyaltyGateway _loyaltyGateway = LoyaltyFactory.CrearClienteFidelizacion(FidelizacionProgramaEnum.Thalamus);

        public FidelizacionModule() : base("api/Thalamus/")
        { 
            const int codigoError = 400;
            const string solicitudDeCancelacion = "Se solicito cancelar la operación";
            const string bodyNoPresente = "El cuerpo de la solicitud no puede ser nulo o vacío";
            
            //Por cuestiones de seguridad, Thalamus rechaza conexiones con protocolos de encriptación inferiores a TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            #region Actions
            Post("persona/Saldo", async (p, token) =>
            {
                var solicitud = RequestHelper.RecuperarSolicitudRecibida<FidelizacionSolicitudThalamusDto>(Request);

                FidelizacionSolicitudResultadoDto response;
                try
                {
                    if (token.IsCancellationRequested)
                        return FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, codigoError, solicitudDeCancelacion);

                    response = await _loyaltyGateway.EnviarBloqueLoyalty(solicitud).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                    HacerLogError(ex);
                }

                return JsonConvert.SerializeObject(response);
            }, null, "Consultar saldo cliente Thalamus");

            Post("Fidelizacion", async (p, token) =>
                {
                    var solicitud = RequestHelper.RecuperarSolicitudRecibida<FidelizacionSolicitudThalamusDto>(Request);

                    FidelizacionSolicitudResultadoDto response;
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
                        }
                    }
                    catch (Exception ex)
                    {
                        response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                        HacerLogError(ex);
                    }

                    return JsonConvert.SerializeObject(response);
                }, null, "Acumulación de puntos Thalamus");

            Post("Redencion", async (p, token) =>
            {
                var solicitud = RequestHelper.RecuperarSolicitudRecibida<FidelizacionSolicitudThalamusDto>(Request);

                FidelizacionSolicitudResultadoDto response;
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
                    }
                }
                catch (AggregateException ex)
                {
                    response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                    HacerLogError(ex.InnerException);
                }
                catch (Exception ex)
                {
                    response = FidelizacionErrorHelper.GenerarErrorEnSolicitud(solicitud, ex);
                    HacerLogError(ex);
                }

                return JsonConvert.SerializeObject(response);
            }, null, "Canje de puntos Thalamus");
         
            #endregion
        }

        #region Private methods

        private static void HacerLogError(Exception ex)
        {
            if (ex != null)
                Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
        }
        #endregion
    }
}