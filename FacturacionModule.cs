using Nancy;
using Nancy.Security;
using System;
using System.Data;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Newtonsoft.Json;
using Vemn.Framework.Logging;
using Nancy.Responses;
using System.Net.Http;
using System.Text;
using Vemn.Framework.ExceptionManagement;
using HostCaldenONNancy.Models;

namespace HostCaldenONNancy.Modules
{
    public class FacturacionModule : NancyModule
    {
        protected Aoniken.CaldenOil.Entidades.Venta ctxVenta;

        public FacturacionModule() : base("api/Facturacion/")
        {
            Get<Response>("GetMovimientoFac", p =>
            {
                try
                {
                    this.RequiresAuthentication();

                    int idMovimientoFac = this.Request.Query["idMovimientoFac"];
                    MovimientoFac mov = HelperEntidades.GetEntity<MovimientoFac>(idMovimientoFac);

                    byte[] buffer =
                        mov.DocumentoConFirmaDigital is byte[]?
                        mov.DocumentoConFirmaDigital :
                        OperacionesDocumento.VistaPreviaCopia(idMovimientoFac);

                    var response = new StreamResponse(() => new System.IO.MemoryStream(buffer), MimeTypes.GetMimeType("copia.pdf"));

                    return response;
                }
                catch (Exception ex)
                {
                    string mensajeError = string.Format("Error en GetMovimientoFac: {0}", ex.Message);

                    if (ex.InnerException != null)
                    {
                        mensajeError += Environment.NewLine + ex.InnerException.Message;

                        if (ex.InnerException.InnerException != null)
                        {
                            mensajeError += Environment.NewLine + ex.InnerException.InnerException.Message;
                        }
                    }

                    mensajeError += Environment.NewLine + Environment.NewLine + ex.StackTrace;

                    Logger.Default.ErrorFormat(mensajeError);

                    byte[] errorBytes = Encoding.UTF8.GetBytes(mensajeError);

                    var errorResponse = new Response()
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Contents = e => e.Write(errorBytes, 0, errorBytes.Length)
                    };

                    return errorResponse;
                }
            }, null, name: "Retorna el PDF de una factura o remito. Parámetros: {idMovimientoFac}");

            Post<Response>("EmitirFacturaORemito", p =>
            {
                try
                {
                    this.RequiresAuthentication();

                    //string venta = this.Request.Form["data"];

                    string venta= new System.IO.StreamReader(this.Request.Body).ReadToEnd();

                    Models.VentaAFacturarORemitir jsonVenta = JsonConvert.DeserializeObject<VentaAFacturarORemitir>(venta);

                    DatosCuponClover cuponClover = new DatosCuponClover();
                    if (jsonVenta!=null && jsonVenta.valores!=null && jsonVenta.valores.Cupones.Count>0)
                    {
                        cuponClover = jsonVenta.valores.Cupones[0].CuponClover;
                    }

                    HidratarContextoDeVentaParaDespuesFacturarlo(ref this.ctxVenta, jsonVenta);

                    Facturador.Emitir(ctxVenta);

                    RespuestaEmitirFacturaORemito respuesta = new RespuestaEmitirFacturaORemito()
                    {
                        IdMovimientoFac = 100200,
                        CAENumero = "12345678901234",
                        CAEFechaVencimiento = HelperFechas.FechaHoraActual().AddDays(30),
                        Prefijo = 1234,
                        Numero = 12345678,
                        cuponClover = cuponClover
                    };

                    var respuestaJson = JsonConvert.SerializeObject(respuesta);

                    //var response = new Response() { StatusCode = HttpStatusCode.OK,  Headers.add };

                    return Response.AsText(respuestaJson, "application/json");

                    //return response;
                }
                catch (Exception ex)
                {
                    Logger.Default.ErrorFormat("Error en EmitirFacturaORemito: {0}", ex.Message);

                    byte[] errorBytes = Encoding.UTF8.GetBytes(ex.Message);

                    var errorResponse = new Response()
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Contents = e => e.Write(errorBytes, 0, errorBytes.Length)
                    };

                    return errorResponse;
                }
            }, null, name: "Emite la factura o remito correspondiente a una venta.");

            Get<Models.FacturasVenta[]>("GetFacturasVenta", p =>
            {
                this.RequiresAuthentication();
                List<Models.FacturasVenta> facturasVentaLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        facturasVentaLista = HelperSQL.GetListaFacturasVentaEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (facturasVentaLista.ToArray());
            }, null, name: "Devuelve la lista de facturas de venta entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Models.FacturasCompra[]>("GetFacturasCompra", p =>
            {
                this.RequiresAuthentication();
                List<Models.FacturasCompra> facturasCompraLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        facturasCompraLista = HelperSQL.GetListaFacturasCompraEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (facturasCompraLista.ToArray());
            }, null, name: "Devuelve la lista de facturas de compra entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");
        }

        private void HidratarContextoDeVentaParaDespuesFacturarlo(ref Aoniken.CaldenOil.Entidades.Venta ctxVenta, VentaAFacturarORemitir jsonVenta)
        {
            Comprobante comprobante = new Comprobante();
            comprobante.MovimientoFac = new MovimientoFac();
            comprobante.MovimientoFac.IdTipoMovimiento = DeterminarTipoMovimiento(jsonVenta.cabecera);
            comprobante.MovimientoFac.Fecha = jsonVenta.cabecera.Fecha;
            comprobante.MovimientoFac.RazonSocial = jsonVenta.cabecera.RazonSocial;
            comprobante.MovimientoFac.NumeroDocumento = jsonVenta.cabecera.NumeroDocumento;
            comprobante.MovimientoFac.Domicilio = jsonVenta.cabecera.Domicilio;
            comprobante.MovimientoFac.IdCliente = jsonVenta.cabecera.IdClienteSeleccionado;
            comprobante.MovimientoFac.IdLocalidad = jsonVenta.cabecera.IdLocalidad;             //  ¿y para qué sirve Cabecera.CodigoPostal?
            comprobante.MovimientoFac.Patente = jsonVenta.cabecera.Patente;
            //  ¿cabecera.TipoPago?
            comprobante.MovimientoFac.NetoNoGravado = jsonVenta.cabecera.NetoNoGravado;
            comprobante.MovimientoFac.NetoMercaderias = jsonVenta.cabecera.NetoGravado;         //  por ahora el NetoGravado que viene, va a NetoMercaderías
                                                                                                //  pero hay que refinar eso según se haya tomado un Despacho,
                                                                                                //  facturado un Lubricante o un cigarrillo

            comprobante.MovimientoFac.IVA = jsonVenta.cabecera.IVA;

            comprobante.MovimientoFac.Total = jsonVenta.cabecera.Total;

            foreach (var renglon in jsonVenta.detalle)
            {
                MovimientoDetalleFac movimientoDetalleFac = new MovimientoDetalleFac();
                movimientoDetalleFac.IdArticulo = renglon.IdArticulo;


                comprobante.MovimientoFac.MovimientosDetalleFacList.Add(movimientoDetalleFac);
            }

            ctxVenta.Comprobantes.Add(comprobante);

        //public string TipoComprobante { get; set; }
        //public string LetraComprobante { get; set; }
        //public int PuntoVenta { get; set; }
        //public int Numero { get; set; }
        //public string Fecha { get; set; }
        //public string RazonSocial { get; set; }
        //public string NumeroDocumento { get; set; }
        //public string Domicilio { get; set; }
        //public string Localidad { get; set; }
        //public int CodigoPostal { get; set; }
        //public string Patente { get; set; }
        //public string Moneda { get; set; }
        //public string TipoPago { get; set; }
        //public decimal NetoGravado { get; set; }
        //public decimal NetoNoGravado { get; set; }
        //public decimal IVA { get; set; }
        //public decimal ImpuestoInterno { get; set; }
        //public decimal Tasas { get; set; }
        //public decimal TasaVial { get; set; }
        //public decimal PercepcionIIBB { get; set; }
        //public decimal PercepcionIVA { get; set; }
        //public decimal OtrasPercepciones { get; set; }
        //public decimal Total { get; set; }
        //public int? IdClienteSeleccionado { get; set; }


        //HidratarDatosCliente(ref ctxVenta, jsonVenta);

        }

        private string DeterminarTipoMovimiento(Cabecera cabecera)
        {
            switch (cabecera.TipoComprobante.ToUpper())
            {
                case "FACTURA":
                    return (cabecera.LetraComprobante.ToUpper().Equals("A") ? "FAA" : "FAB");
                default:
                    throw new ApplicationException("Tipo de comprobante no soportado: " + cabecera.TipoComprobante);
            }
        }

        private void HidratarDatosCliente(ref Aoniken.CaldenOil.Entidades.Venta ctxVenta, VentaAFacturarORemitir jsonVenta)
        {
            ctxVenta.Cliente = new Aoniken.CaldenOil.Entidades.Cliente();
            ctxVenta.Cliente.RazonSocial = jsonVenta.cabecera.RazonSocial;
            ctxVenta.Cliente.NumeroDocumento = jsonVenta.cabecera.NumeroDocumento;
            ctxVenta.Cliente.Calle = jsonVenta.cabecera.Domicilio;
            //    ctxVenta.Cliente.IdLocalidad = jsonVenta.cabecera.Localidad;


            //public string Localidad { get; set; }
            //public int CodigoPostal { get; set; }
        }
    }
}
