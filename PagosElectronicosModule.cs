using Aoniken.CaldenOil.Entidades.Modelos.Enums.PagosElectronicos;
using EntidadesPagosElectronicos.DTOs;
using EntidadesPagosElectronicos.Modelos.Api;
using EntidadesPagosElectronicos.Modelos.MercadoPago.MerchantOrders.Confirmations;
using EntidadesPagosElectronicos.Modelos.Trafigura.Transactions;
using IntegracionUtiles;
using Nancy;
using Nancy.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public sealed class PagosElectronicosModule : NancyModule
    {
        private const string MimeType = "application/json";
        private const string ErrorRespuestaVacia = "La respuesta obtenida no contiene informacion alguna";

        public PagosElectronicosModule() : base("api/PagosElectronicos/")
        {
            Post("payment/confirmation", async (p, token) =>
            {
                object response = await ProcesarSolicitudHttp(token).ConfigureAwait(false);
                return JsonConvert.SerializeObject(response);
            });

            Post("payment/cancel", async (p, token) =>
            {
                object response = await ProcesarSolicitudHttp(token).ConfigureAwait(false);
                return JsonConvert.SerializeObject(response);
            });
        }

        private async Task<object> ProcesarSolicitudHttp(CancellationToken token)
        {
            ConfirmationDto confirmationDto = GetObjectConfirmationFromRequestBody();
            ApiResponse response;
            try
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                string endpoint = confirmationDto.NotificationEndpoint;
                response = await SendObjectConfirmationV2Async(endpoint, confirmationDto.ConfirmationObjToSend, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                response = CrearErrorDto(ex.Message);
                Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
            }

            return MapResponseToProveedorPagoResponse(response, confirmationDto.TipoProveedor);
        }

        private object MapResponseToProveedorPagoResponse(ApiResponse response, TipoProveedorEnum tipoProveedor)
        {
            object mappedResponse;
            switch (tipoProveedor)
            {
                case TipoProveedorEnum.MercadoLibre:
                    mappedResponse = MapResponseToMerchantOrderDto(response);
                    break;
                case TipoProveedorEnum.Trafigura:
                    mappedResponse = MapResponseToTrafiguraPaymentConfirmationDto(response);
                    break;
                default:
                    throw new ArgumentException("El proveedor indicado no admite confirmaciones");
            }

            return mappedResponse;
        }

        private ConfirmationDto GetObjectConfirmationFromRequestBody()
        {
            ConfirmationDto confirmation;
            using (RequestStream objConfirmation = RequestStream.FromStream(Request.Body))
            {
                confirmation = JsonConvert.DeserializeObject<ConfirmationDto>(objConfirmation.ReadAsString());
            }

            return confirmation;
        }

        private async Task<ApiResponse> SendObjectConfirmationV2Async(string endpoint, object bodyObject, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(endpoint))
                return CrearErrorDto("El endpoint indicado es invalido para ejecutar la accion http");

            if (bodyObject is null)
                return CrearErrorDto("El objeto body a enviar no es valido");

            ApiResponse response;
            try
            {
                string confirmationContent = JsonConvert.SerializeObject(bodyObject);
                Logger.Default.Info($"Endpoint utilizado para el envio del request es {endpoint}");
                Logger.Default.Info($"Json to send: {confirmationContent}");
                using (StringContent body = new StringContent(confirmationContent, Encoding.UTF8, MimeType))
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage confirmationResult = await client.PostAsync(endpoint, body, token).ConfigureAwait(false);
                    string content = await confirmationResult.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Logger.Default.Info($"received json: {content}");
                    response = ParseConfirmationResponse(confirmationResult, content); 
                }
            }
            catch (Exception ex)
            {
                response = CrearErrorDto($"Ocurrió un error durante el proceso de confirmación. Detalle: {ex.Message}");
                Logger.Default.Error(ExceptionManager.GetExceptionStringNoAssemblies(ex));
            }

            return response;
        }

        /// <summary> Parsear la respuesta recibida desde la nube AOK </summary>
        /// <param name="responseMessage"> Objeto HttpResponseMessage con el resultado de la operacion REST </param>
        /// <param name="content"> Cadena de texto que representa el body recibido desde la nube </param>
        /// <returns></returns>
        private ApiResponse ParseConfirmationResponse(HttpResponseMessage responseMessage, string content)
        {
            ApiResponse response;
            if (responseMessage is null)
            {
                response = CrearErrorDto("La respuesta obtenida desde la nube no es la esperada");
            }
            else if (responseMessage.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                response = CrearErrorDto("Error no recuperable al enviar la solicitud indicada. El servidor devolvió error interno");
            }
            else if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response = CrearErrorDto("El servidor no autoriza la ejecución del request actual");
            }
            else if (!responseMessage.IsSuccessStatusCode)
            {
                response = string.IsNullOrWhiteSpace(content)
                    ? CrearErrorDto($"El contenido recibido es nulo o vacio. Estado de respuesta obtenida es : {responseMessage.StatusCode}")
                    : JsonConvert.DeserializeObject<ApiResponse>(content);
            }
            else
            {
                response = !string.IsNullOrWhiteSpace(content)
                    ? JsonConvert.DeserializeObject<ApiResponse>(content)
                    : new ApiResponse { Success = responseMessage.StatusCode };
            }

            return response;
        }

        private MerchantOrderConfirmationResponseDto MapResponseToMerchantOrderDto(ApiResponse response)
        {
            MerchantOrderConfirmationResponseDto dto;
            if (response is null)
            {
                dto = new MerchantOrderConfirmationResponseDto(-1, null, ErrorRespuestaVacia);
            }
            else if (response.Success != null)
            {
                dto = JToken.FromObject(response.Success).ToObject<MerchantOrderConfirmationResponseDto>();
            }
            else if (response.Error is null)
            {
                dto = new MerchantOrderConfirmationResponseDto(-1, null, ErrorRespuestaVacia);
            }
            else
            {
                ErrorResponse error = JsonUtils.ConvertTo<ErrorResponse>(JToken.FromObject(response.Error));
                dto = string.IsNullOrWhiteSpace(error.ErrorDetails?.Message)
                    ? new MerchantOrderConfirmationResponseDto(-1, null, ErrorRespuestaVacia)
                    : new MerchantOrderConfirmationResponseDto(-1, null, error.ErrorDetails.Message);
            }

            return dto;
        }

        private TrafiguraPaymentConfirmationResponseDto MapResponseToTrafiguraPaymentConfirmationDto(ApiResponse response)
        {
            TrafiguraPaymentConfirmationResponseDto dto;
            if (response is null)
            {
                dto = new TrafiguraPaymentConfirmationResponseDto(ErrorRespuestaVacia);
            }
            else if (response.Error != null)
            {
                ApiErrorResponse error = JToken.FromObject(response.Error).ToObject<ApiErrorResponse>();
                dto = error is null
                    ? new TrafiguraPaymentConfirmationResponseDto(ErrorRespuestaVacia)
                    : string.IsNullOrWhiteSpace(error.ErrorDetails?.Message)
                        ? new TrafiguraPaymentConfirmationResponseDto(ErrorRespuestaVacia)
                        : new TrafiguraPaymentConfirmationResponseDto(error.ErrorDetails.Message);
            }
            else
            {
                dto = new TrafiguraPaymentConfirmationResponseDto(true);
            }

            return dto;
        }

        private ApiResponse CrearErrorDto(string msg)
        {
            return new ApiResponse { Error = new ErrorResponse(msg) };
        }
    }
}