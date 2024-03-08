using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Vemn.Framework.Logging;
using System.Text;
using NPoco.RowMappers;
using System.Net;
using System.Linq;
using Nancy.Responses;

namespace HostCaldenONNancy.Modules
{
    public class ClientesModule : NancyModule
    {
        public ClientesModule() : base("api/Clientes/")
        {
            Get<Models.Cliente[]>("GetClientesPorRazonSocial", p =>
            {
                this.RequiresAuthentication();
                string razonsocial = this.Request.Query["razonsocial"];
                List<Models.Cliente> clientesLista = HelperSQL.GetListaClientes(razonsocial);                
                return (clientesLista.ToArray());
            }, null, name: "Devuelve una lista de clientes filtrada por código, razón social, CUIT , CUIL o DNI. Parámetros: {razonsocial}");

            Get<Models.Cliente[]>("GetClientes", p =>
            {
                this.RequiresAuthentication();
                int parametro = this.Request.Query["soloLosQueTienenMovimientosEnCtaCte"];
                DateTime? ultimaFechaActualizacion = null;
                if (this.Request.Query["ultimaFechaActualizacion"].Value != null)
                {
                    ultimaFechaActualizacion = this.Request.Query["ultimaFechaActualizacion"];
                }
                bool soloLosQueTienenMovimientosEnCtaCte = true;
                if (parametro == 0)
                {
                    soloLosQueTienenMovimientosEnCtaCte = false;
                } else if (parametro == 1)
                {
                    soloLosQueTienenMovimientosEnCtaCte = true;
                }
                List<Models.Cliente> clientesLista = HelperSQL.GetListaClientes(soloLosQueTienenMovimientosEnCtaCte, ultimaFechaActualizacion);
                return (clientesLista.ToArray());
            }, null, name: "Devuelve la lista de todos los clientes, seleccionando opcionalmente sólo los que tienen algún movimiento en la cta.cte. Parámetros opcionales: {soloLosQueTienenMovimientosEnCtaCte} {ultimaFechaActualizacion}");

            Get <Models.ClienteArticuloPrecio[]>("GetPreciosEspeciales", p =>
            {
                // this.RequiresAuthentication();
                int idVendedor = this.Request.Query["idVendedor"];

                List<Models.ClienteArticuloPrecio> lista = new List<Models.ClienteArticuloPrecio>();

                List<Models.Cliente> clientesLista = HelperSQL.GetListaClientes(idVendedor);
                List<Models.ArticuloInfo> articulosLista = HelperSQL.GetListaArticulosHabilitados();

                Venta ctxVenta = new Venta();
                ctxVenta.ConfiguracionFacturacion = HelperConfiguracionFacturacion.BuscarConfiguracionFacturacion();
                ctxVenta.ConfiguracionCliente = HelperConfiguracionCliente.BuscarConfiguracionCliente();

                foreach (Models.Cliente c in clientesLista)
                {                                        
                    Cliente cliente = HelperCliente.BuscarCliente(c.IdCliente);
                    ctxVenta.Cliente = HelperCliente.BuscarCliente(cliente.IdCliente);

                    foreach (Models.ArticuloInfo a in articulosLista)
                    {
                        Articulo articulo = HelperArticulo.BuscarArticulo(a.IdArticulo);
                        decimal precioCalculadoUnitario = 0;
                        decimal precioListaUnitario = 0;
                        DescuentosArticuloCalculados precioCalculado = Facturador.CalculoPrecio(articulo, 1, ctxVenta);
                        precioCalculadoUnitario = precioCalculado.PrecioUnitario;

                        if (cliente.NumeroListaPrecios == 0)
                        {
                            precioListaUnitario = articulo.PrecioPublico;
                        }
                        else
                        {
                            DescuentosArticuloCalculados precioLista = Facturador.CalcularPrecioPorListaDePrecios(cliente, articulo, new StringBuilder());
                            precioListaUnitario = precioLista.PrecioUnitario;
                        }
                                                                        
                        if (precioCalculadoUnitario != precioListaUnitario)
                        {
                            Models.ClienteArticuloPrecio row = new Models.ClienteArticuloPrecio
                            {
                                IdArticulo = articulo.IdArticulo,
                                IdCliente = cliente.IdCliente,
                                Precio = precioCalculado.PrecioUnitario
                            };
                            lista.Add(row);
                        }
                    }
                }
                return lista.ToArray();
            }, null, name: "(uso interno)");

            Get<Models.InformacionSaldoCliente[]>("GetSaldosCtaCteClientes", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionSaldoCliente> listaSaldos = new List<Models.InformacionSaldoCliente>();
                try
                {
                    int? idCliente = this.Request.Query["idCliente"];
                    listaSaldos = HelperSQL.GetSaldosCtaCteClientes(idCliente ?? 0);
                }
                catch
                { }
                return (listaSaldos.ToArray());
            }, null, name: "Devuelve la lista de uno o todos los clientes con sus saldos en cta.cte. Parámetros: {idCliente, si es cero o vacío=todos}");

            Get<Models.InformacionRecibo[]>("GetRecibosCtaCteClientes", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionRecibo> listaRecibos = new List<Models.InformacionRecibo>();
                try
                {
                    int? idCliente = this.Request.Query["idCliente"];
                    System.DateTime? desdeFecha = this.Request.Query["desdeFecha"];
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];
                    if (desdeFecha != null && hastaFecha != null)
                    {
                        listaRecibos = HelperSQL.GetRecibosCtaCteClientes(idCliente ?? 0, desdeFecha.Value, hastaFecha.Value);
                    }
                }
                catch
                { }                                
                return (listaRecibos.ToArray());
            }, null, name: "Devuelve la lista de recibos en cta.cte. de uno o todos los clientes, en un intervalo de fechas dado. Parámetros: {idCliente, si es cero o vacío=todos} {desdeFecha} {hastaFecha}");

            Get<Response>("GetRecibo", p =>
            {
                try
                {
                    this.RequiresAuthentication();

                    int idRecibo = this.Request.Query["idRecibo"];

                    byte[] buffer = OperacionesDocumento.VistaPreviaCopiaRecibo(idRecibo);

                    var response = new StreamResponse(() => new System.IO.MemoryStream(buffer), MimeTypes.GetMimeType("copia.pdf"));

                    return response;
                }
                catch (Exception ex)
                {
                    Logger.Default.ErrorFormat("Error en GetRecibo: {0}", ex.Message);

                    byte[] errorBytes = Encoding.UTF8.GetBytes(ex.Message);

                    var errorResponse = new Response()
                    {
                        StatusCode = Nancy.HttpStatusCode.InternalServerError,
                        Contents = e => e.Write(errorBytes, 0, errorBytes.Length)
                    };

                    return errorResponse;
                }
            }, null, name: "Retorna el PDF de un recibo. Parámetro: {idRecibo}");

            Get<Models.InformacionMorosidad[]>("GetMorosidadClientes", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionMorosidad> listaMorosidad = new List<Models.InformacionMorosidad>();
                try
                {
                    int? idCliente = this.Request.Query["idCliente"];
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];                    
                    listaMorosidad = HelperSQL.GetMorosidadClientes(idCliente ?? 0, hastaFecha ?? System.DateTime.Now);               
                }
                catch
                { }
                return (listaMorosidad.ToArray());
            }, null, name: "Devuelve la lista de morosidad en cta.cte. de uno o todos los clientes. Parámetros: {idCliente, si es cero o vacío=todos} {hastaFecha, vacía=hasta hoy}");

            Get<Models.InformacionDetalleSaldos[]>("GetDetalleSaldosCuentaCorriente", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionDetalleSaldos> listaDetalleSaldos = new List<Models.InformacionDetalleSaldos>();
                try
                {
                    int? idCliente = this.Request.Query["idCliente"];
                    System.DateTime? hastaFecha = this.Request.Query["fechaHasta"];
                    listaDetalleSaldos = HelperSQL.GetDetalleSaldosCuentaCorriente(idCliente ?? 0, hastaFecha ?? System.DateTime.Now);
                }
                catch
                { }
                return (listaDetalleSaldos.ToArray());
            }, null, name: "Devuelve la lista de detalle de saldos en cta.cte. de uno o todos los clientes. Parámetros: {idCliente, si es cero o vacío=todos} {hastaFecha, vacía=hasta hoy}");

            Get<Models.LimiteExcedido[]>("GetLimitesExcedidos", p =>
            {
                this.RequiresAuthentication();
                List<Models.LimiteExcedido> listaLimitesExcedidos = HelperSQL.GetLimitesExcedidos(paraNotificarCaldenON: false);                
                return (listaLimitesExcedidos.ToArray());
            }, null, name: "(uso interno)");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = Nancy.HttpStatusCode.BadRequest };
            });

            Put<bool>("AutorizarVenta", p =>
            {
                bool result = true;
                
                try{
                    string idVenta = this.Request.Query["idVenta"];
                    Logger.Default.DebugFormat("PUT: AutorizarVenta");
                    //IndexModule.LoguearRequest(this.Request);
                    //IndexModule.LoguearRequestQuery(this.Request.Query);
                    Logger.Default.DebugFormat("idVenta: {0}", idVenta);
                    if (String.IsNullOrEmpty(idVenta))
                    {
                        result = false;
                    }
                    else
                    {
                        //Estado: 0-Pendiente 1-Aceptado 2-Rechazado 3-Sin respuesta 4-Abortado por el operador
                        HelperSQL.ActualizarVenta(estado: 1, idVenta: idVenta);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(Vemn.Framework.ExceptionManagement.ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    result = false;
                }
                return (result);
            }, null, name: "(uso interno)");

            Put<bool>("AmpliarLimite", p =>
            {
                bool result = true;
                try
                {
                    int? idVenta = this.Request.Query["idVenta"];
                    int? tipoVenta = this.Request.Query["tipoVenta"];
                    int? nuevoLimite = this.Request.Query["nuevoLimite"];
                    Logger.Default.DebugFormat("PUT: AmpliarLimite");
                    Logger.Default.DebugFormat("idVenta: {0}", idVenta);
                    Logger.Default.DebugFormat("tipoVenta: {0}", tipoVenta);
                    Logger.Default.DebugFormat("nuevoLimite: {0}", nuevoLimite);
                }
                catch
                {
                    result = false;
                }
                return (result);
            }, null, name: "(uso interno)");

            Put<bool>("RechazarOperacion", p =>
            {
                bool result = true;
                try
                {
                    string idVenta = this.Request.Query["idVenta"];
                    Logger.Default.DebugFormat("PUT: RechazarOperacion");
                    //IndexModule.LoguearRequest(this.Request);
                    //IndexModule.LoguearRequestQuery(this.Request.Query);
                    Logger.Default.DebugFormat("idVenta: {0}", idVenta);
                    if (String.IsNullOrEmpty(idVenta))
                    {
                        result = false;
                    }
                    else
                    {
                        //Estado: 0-Pendiente 1-Aceptado 2-Rechazado 3-Sin respuesta 4-Abortado por el operador
                        HelperSQL.ActualizarVenta(estado: 2, idVenta: idVenta);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(Vemn.Framework.ExceptionManagement.ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    result = false;
                }
                return (result);
            }, null, name: "(uso interno)");

            Get<decimal>("CalcularPrecio", p =>
            {
                int idCliente = this.Request.Query["idCliente"];
                int idArticulo = this.Request.Query["idArticulo"];
                int cantidad = this.Request.Query["cantidad"];
                int nroListaPrecio = this.Request.Query["nroListaPrecio"];

                Articulo articulo = HelperArticulo.BuscarArticulo(idArticulo);
                Cliente cliente = HelperCliente.BuscarCliente(idCliente);
                cliente.NumeroListaPrecios = nroListaPrecio;

                Venta ctxVenta = new Venta();
                ctxVenta.Cliente = cliente;
                ctxVenta.ConfiguracionFacturacion = HelperConfiguracionFacturacion.BuscarConfiguracionFacturacion();
                ctxVenta.ConfiguracionCliente = HelperConfiguracionCliente.BuscarConfiguracionCliente();

                DescuentosArticuloCalculados precio = Facturador.CalculoPrecio(articulo, cantidad, ctxVenta);
                
                return precio.PrecioUnitario;
            }, null, name: "Retorna el precio que se le haría a un cliente con los parámetros indicados. Parámetros: {idCliente} {idArticulo} {cantidad} {nroListaPrecio}");

            Put<bool>("AddEmail", p =>
            {
                bool result = true;
                try
                {
                    string idCliente = this.Request.Form["IdCliente"];

                    string nuevoEmail = this.Request.Form["NuevoEmail"];

                    //Logger.Default.DebugFormat("PUT: RechazarOperacion");
                    //IndexModule.LoguearRequest(this.Request);
                    //IndexModule.LoguearRequestQuery(this.Request.Query);
                    if (String.IsNullOrEmpty(idCliente) || String.IsNullOrEmpty(nuevoEmail))
                    {
                        result = false;
                    }
                    else
                    {
                        result = HelperSQL.ActualizarCliente_ActivarEnviaResumenPorMailYPonerEmail(idCliente, nuevoEmail);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(Vemn.Framework.ExceptionManagement.ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    result = false;
                }
                return (result);
            }, null, name: "(uso interno)");

            Get<Aoniken.CaldenOil.Entidades.Modelos.AFIP.InfoCUIT>("GetInformacionCUIT", p =>
            {
                var remoteIpAddress = Request.UserHostAddress;
                var isInternalIp = IsInternalIpAddress(remoteIpAddress);

                if (isInternalIp)
                {
                    Aoniken.CaldenOil.Entidades.Modelos.AFIP.InfoCUIT infoCUIT;

                    try
                    {
                        string cuit = this.Request.Query["CUIT"];
                        infoCUIT = LibreariaAFIP.RecuperarInfoCUIT(cuit);
                    }
                    catch (Exception ex)
                    {
                        infoCUIT = new Aoniken.CaldenOil.Entidades.Modelos.AFIP.InfoCUIT();
                        infoCUIT.textoError = ex.Message + Environment.NewLine + ex.StackTrace;
                    }
                    return (infoCUIT);
                }
                else
                {
                    this.RequiresAuthentication();

                    var respuesta =new Aoniken.CaldenOil.Entidades.Modelos.AFIP.InfoCUIT();
                    respuesta.textoError = "puesto no habilitado";
                    return respuesta;
                }
            }, null, name: "(uso interno)");

            Get<Models.Localidad[]>("GetLocalidades", p => 
            {
                this.RequiresAuthentication();
                List<Models.Localidad> listaLocalidades = HelperSQL.GetLocalidades();
                return listaLocalidades.ToArray();
            }
            , null, "Devuelve la lista de todas las localidades.");
        }

        private bool IsInternalIpAddress(string remoteIpAddress)
        {
            if (remoteIpAddress=="::1")
            {
                return true;
            }

            // Define a list of internal IP address ranges
            var internalIpRanges = new List<string>
            {
                "10.0.0.0/8",
                "172.16.0.0/12",
                "192.168.0.0/16"
            };

            // Parse the IP address and check if it falls within any of the internal ranges
            var ip = IPAddress.Parse(remoteIpAddress);
            return internalIpRanges.Any(range => new IPNetwork(IPAddress.Parse(IpDeRed(range)),PrefijoRed(range)).Contains(ip));
        }

        /// <summary>
        /// Extrae la cantidad de bits de una red.
        /// Ej: de la red "10.0.0.0/8" devuelve 8
        /// </summary>
        /// <param name="red"></param>
        /// <returns></returns>
        private int PrefijoRed(string red)
        {
            string prefijo = red.Substring(red.IndexOf("/")+1);
            return int.Parse(prefijo);
        }

        private string IpDeRed(string range)
        {
            string ip = range.Substring(0, range.IndexOf("/"));
            return ip;
        }

        private class IPNetwork
        {
            public IPNetwork(IPAddress prefix, int prefixLength)
            {
                Prefix = prefix;
                PrefixLength = prefixLength;
                PrefixBytes = Prefix.GetAddressBytes();
                Mask = CreateMask();
            }

            public IPAddress Prefix { get; }

            private byte[] PrefixBytes { get; }

            /// <summary>
            /// The CIDR notation of the subnet mask 
            /// </summary>
            public int PrefixLength { get; }

            private byte[] Mask { get; }

            public bool Contains(IPAddress address)
            {
                if (Prefix.AddressFamily != address.AddressFamily)
                {
                    return false;
                }

                var addressBytes = address.GetAddressBytes();
                for (int i = 0; i < PrefixBytes.Length && Mask[i] != 0; i++)
                {
                    if (PrefixBytes[i] != (addressBytes[i] & Mask[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private byte[] CreateMask()
            {
                var mask = new byte[PrefixBytes.Length];
                int remainingBits = PrefixLength;
                int i = 0;
                while (remainingBits >= 8)
                {
                    mask[i] = 0xFF;
                    i++;
                    remainingBits -= 8;
                }
                if (remainingBits > 0)
                {
                    mask[i] = (byte)(0xFF << (8 - remainingBits));
                }

                return mask;
            }
        }
    }
}