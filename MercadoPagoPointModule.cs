using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Nancy;
using Nancy.IO;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;
using EntidadesPagosElectronicos.Modelos.MercadoPago.Point;

namespace HostCaldenONNancy.Modules
{
    public sealed class MercadoPagoPointModule : NancyModule
    {
        private readonly ConfiguracionIntegraciones _config = HelperConfiguracionIntegracion.BuscarConfiguracionIntegraciones();
        private readonly object _locker = new object();

        public enum BaseAddressEnum
        {
            Testing = 0,
            Production = 1,
            Staging = 2,
            Mock = 3
        }

        public MercadoPagoPointModule() : base("api/MercadoPagoPoint/")
        {
            const int codigoError = 400;
            const string solicitudDeCancelacion = "Se solicito cancelar la operación";
            const string bodyNoPresente = "El cuerpo de la solicitud no puede ser nulo o vacío";

            //Por cuestiones de seguridad, Thalamus rechaza conexiones con protocolos de encriptación inferiores a TLS 1.2
            //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            #region Actions
            Get("ObtenerDispositivos", async (p, token) =>
                {
                    object respuesta;
                    try
                    {
                        Logger.Default.Info("Ejecutando ObtenerDispositivos");
                        int legacySiteId = _config.IntegracionMercadoPagoSiteID.Value;
                        string endpoint = $"{CreateBaseUrlToSend(BaseAddressEnum.Production)}/api/MercadoPagoPoint/devices/{legacySiteId}";
                        Logger.Default.Info($"Endpoint: {endpoint}");
                        using (var client = new RestClient.RestClientDotNet())
                        {
                            HttpResponseMessage response = client.Get(endpoint, null);
                            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Logger.Default.Info($"Json recibido: {content}");
                            if (response.IsSuccessStatusCode)
                            {
                                respuesta = JsonConvert.DeserializeObject<Devices>(content);
                            }
                            else
                            {
                                respuesta = new PointError { Status = (int)response.StatusCode, Message = content };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        respuesta = new PointError { Status = codigoError, Message = ex.Message };
                        HacerLogError(ex);
                    }
                    return JsonConvert.SerializeObject(respuesta);
                }, null, "Obtener Dispositivos Point");

            Patch("CambiarModoOperacion", async (p, token) =>
            {
                string respuesta = String.Empty;
                try
                {
                    string deviceId = Request.Query["deviceId"];
                    string modoOperacion = Request.Query["modoOperacion"];
                    int legacySiteId = _config.IntegracionMercadoPagoSiteID.Value;
                    ModoOperativoDTO modoOperativoDTO = new ModoOperativoDTO { DeviceId = deviceId, LegacySiteId = legacySiteId, OperatingMode = modoOperacion };
                    string jsonContent = JsonConvert.SerializeObject(modoOperativoDTO);
                    string endpoint = $"{CreateBaseUrlToSend(BaseAddressEnum.Production)}/api/MercadoPagoPoint/devices/OperatingMode";
                    Logger.Default.Info($"Endpoint: {endpoint}");
                    using (var client = new RestClient.RestClientDotNet())
                    {
                        if (client.Patch(endpoint, jsonContent, null))
                        {
                            respuesta = $"El cambio a modo {modoOperacion} se hizo exitosamente";
                        }
                        else
                        {
                            respuesta = "No se pudo realizar el cambio";
                        }
                    }
                }
                catch (AggregateException ex)
                {
                    Exception inner = ex.InnerException;
                    HacerLogError(inner);
                }
                catch (Exception ex)
                {
                    HacerLogError(ex);
                }

                return respuesta;
            }, null, "Cambiar Modo Operacion Point");

            Delete("CancelarOperacion", async (p, token) =>
            {
                object respuesta;
                HttpStatusCode statusCode;
                try
                {
                    Logger.Default.Info("Ejecutando CancelarOperacion");
                    int legacySiteId = _config.IntegracionMercadoPagoSiteID.Value;
                    Guid externalReference = (Guid)Request.Query["externalReference"];
                    string endpoint = $"{CreateBaseUrlToSend(BaseAddressEnum.Production)}/api/MercadoPagoPoint/payment/Intention/rollback/{legacySiteId}?externalReference={externalReference}";
                    Logger.Default.Info($"Endpoint: {endpoint}");                    
                    using (var client = new RestClient.RestClientDotNet())
                    {
                        HttpResponseMessage response = client.Delete(endpoint, String.Empty);
                        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Logger.Default.Info($"Json recibido: {content}");
                        if (response.IsSuccessStatusCode)
                        {
                            respuesta = content;
                            statusCode = HttpStatusCode.OK;
                        }
                        else
                        {
                            respuesta = new PointError { Status = (int)response.StatusCode, Message = content };
                            statusCode = HttpStatusCode.BadRequest;
                        }
                    }
                }
                catch (Exception ex)
                {
                    respuesta = new PointError { Status = codigoError, Message = ex.Message };
                    statusCode = HttpStatusCode.BadRequest;
                    HacerLogError(ex);
                }
                return Response.AsJson(respuesta, statusCode);
                //return JsonConvert.SerializeObject(respuesta);
            }, null, "Obtener Dispositivos Point");
            #endregion
        }

        #region Private methods

        /// <summary> Crear la url a ser utilizada para el envío del Health Monitor ACK </summary>
        /// <returns> Un string que representa la URL a donde enviar el ACK </returns>
        private static string CreateBaseUrlToSend(BaseAddressEnum workingMode)
        {
            const string testingBaseAddressStr = "https://mercadopagotesting.azurewebsites.net";
            const string productionBaseAddresStr = "https://pagos.caldenoil.com";
            const string stagingBaseAddressStr = "https://mercadopagoproduccion-mercadopagoproduccionstagingslot.azurewebsites.net";
            string baseUrl;
            switch (workingMode)
            {
                case BaseAddressEnum.Testing:
                    {
                        baseUrl = testingBaseAddressStr;
                        break;
                    }
                case BaseAddressEnum.Production:
                    {
                        baseUrl = productionBaseAddresStr;
                        break;
                    }
                case BaseAddressEnum.Staging:
                    {
                        baseUrl = stagingBaseAddressStr;
                        break;
                    }
                default:
                    {
                        baseUrl = string.Empty;
                        break;
                    }
            }

            return baseUrl;
        }

        private async Task<HttpResponseMessage> SendPointRequest(string endpoint, HttpMethod method, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException(nameof(endpoint));

            uint counter = 0;
            bool canRetry = true;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage responseMessage = null;
                while (canRetry)
                {
                    counter++;
                    responseMessage = await ExecuteAsyncHttpMethod(client, endpoint, method, ct).ConfigureAwait(false);
                    canRetry = responseMessage != null
                               && responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized
                               && counter < 2;
                }
                return responseMessage;
            }
        }

        private async Task<HttpResponseMessage> ExecuteAsyncHttpMethod(HttpClient client, string endpoint,
                                                                       HttpMethod method, CancellationToken ct)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException(nameof(endpoint));

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(method, endpoint))
            {
                requestMessage.Content = GetRequestBodyFromContext(method);
                return await client.SendAsync(requestMessage, ct).ConfigureAwait(false);
            }
        }

        private HttpContent GetRequestBodyFromContext(HttpMethod method)
        {
            if (method is null)
                throw new ArgumentException(nameof(method));

            if (method != HttpMethod.Post || Request.Body is null)
                return null;

            HttpContent httpContent;
            using (RequestStream requestStream = RequestStream.FromStream(Request.Body))
            {
                const string mediaType = "application/json";
                string body = requestStream.ReadAsString();
                httpContent = new StringContent(body, System.Text.Encoding.UTF8, mediaType);
            }

            return httpContent;
        }

        private static void HacerLogError(Exception ex)
        {
            if (ex != null)
                Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
        }
        #endregion
    }
}