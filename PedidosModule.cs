using Nancy;
using Nancy.Security;
using System;
using System.Data;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Newtonsoft.Json;
using Aoniken.CaldenOil.Common;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class PedidosModule : NancyModule
    {       
        private decimal RecalcularTotal(dynamic jsonPedido, Cliente cliente)
        {
            decimal totalPedido = 0;
            foreach (var item in jsonPedido)
            {
                if (item.precioAproximado == true)
                {
                    int idArticulo = item.idArticulo;
                    int cantidad = 1;

                    Articulo articulo = HelperArticulo.BuscarArticulo(idArticulo);

                    Venta ctxVenta = new Venta
                    {
                        Cliente = cliente,
                        ConfiguracionFacturacion = HelperConfiguracionFacturacion.BuscarConfiguracionFacturacion(),
                        ConfiguracionCliente = HelperConfiguracionCliente.BuscarConfiguracionCliente()
                    };

                    DescuentosArticuloCalculados precioCalculado = Facturador.CalculoPrecio(articulo, cantidad, ctxVenta);
                    item.precio = precioCalculado.PrecioUnitario;
                }

                totalPedido += ((decimal)item.precio - ((decimal)item.precio * (decimal)item.descuento / 100) )* (decimal)item.cantidad;
            }
            return totalPedido;
        }

        public PedidosModule() : base("api/Pedidos/")
        {
            Get<Models.Pedido[]>("GetPedidos", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetPedidos", null);
                this.RequiresAuthentication();
                int idEmpleado = this.Request.Query["idEmpleado"];
                List<Models.Pedido> listaPedidos = HelperSQL.GetListaPedidos(idEmpleado);

                return (listaPedidos.ToArray());
            }, null, name: "Retorna una lista de todos los pedidos no facturados asociados a un empleado. Parámetros: {idEmpleado}");

            Get<Models.ArticuloPedido[]>("GetPedidoDetallado", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetPedidoDetallado", null);
                this.RequiresAuthentication();
                int idPedido = this.Request.Query["idPedido"];
                List<Models.ArticuloPedido> listaPedidos = HelperSQL.GetArticulosPedido(idPedido);

                return (listaPedidos.ToArray());
            }, null, name: "Retorna el detalle de un pedido. Parámetros: {idPedido}");

            Get <Models.Respuesta>("ControlarTalonario", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "ControlarTalonario", null);
                this.RequiresAuthentication();                
                                
                Talonario[] tal_list = OperacionesDocumento.BuscarTalonariosEnElPuesto("PED", false);
                if(tal_list == null || tal_list.Length == 0)
                {
                    //ContextoApp ctxAplicacion = ContextoApp.Instanciar();
                    //string puesto = ctxAplicacion.Puesto.Nombre;

                    return new Models.Respuesta
                    {
                        Valor = false,
                        //Texto = $"No hay un talonario definido para Pedidos en el puesto {puesto}"
                        Texto = $"No hay un talonario definido para Pedidos en el puesto {FacturadorConfiguraciones.NombrePuesto}"
                    };                    
                }
                else
                {
                    return new Models.Respuesta
                    {
                        Valor = true,
                        Texto = ""
                    };
                }
            }, null, name: "(uso interno)");

            Post<Models.Respuesta>("PersistirPedido", p =>
            {
                try
                {
                    #region CodigoAnterior
                    //if (Global.ModoTraza)
                    //{
                    //    Logger.Default.DebugFormat("PersistirPedido:");
                    //    Logger.Default.DebugFormat("    body: '{0}'", TextHelper.BodyToString(this.Request));
                    //    Logger.Default.DebugFormat("    p: '{0}'", p);
                    //    Nancy.DynamicDictionary dict = p as Nancy.DynamicDictionary;
                    //    Logger.Default.DebugFormat("    p.Count: '{0}'", dict.Count);
                    //    //for (int i = 0; i < dict.Count; i = i + 1)
                    //    foreach (string key in dict.Keys)
                    //    {
                    //        Logger.Default.DebugFormat("      dict[{0}]: '{1}'", key, dict[key]);
                    //    }
                    //    Logger.Default.DebugFormat("    this.Request.Form: '{0}'", this.Request.Form);
                    //    dict = this.Request.Form as Nancy.DynamicDictionary;
                    //    Logger.Default.DebugFormat("    this.Request.Form.Count: '{0}'", dict.Count);
                    //    foreach (string key in dict.Keys)
                    //    {
                    //        Logger.Default.DebugFormat("      dict[{0}]: '{1}'", key, dict[key]);
                    //    }
                    //    string value = this.Request.Form["data"] == null ? "NULL" : (string)this.Request.Form["data"];
                    //    Logger.Default.DebugFormat("    this.Request.Form[data]: '{0}'", value);                        
                    //} 
                    #endregion CodigoAnterior

                    TextHelper.LoggearMetodo(p, this.Request, "PersistirPedido", "data");
                    this.RequiresAuthentication();

                    #region CodigoAnterior
                    //int idEmpleado = this.Request.Form["idEmpleado"];
                    //int idCliente = this.Request.Form["idCliente"];
                    //DateTime fechaVencimiento = this.Request.Form["fechaVencimiento"];
                    //string observaciones = this.Request.Form["observaciones"];

                    //int numeroPrecio = this.Request.Form["numeroPrecio"];

                    //string totalPedidoString = this.Request.Form["totalPedido"];
                    ////totalPedidoString = totalPedidoString.Replace(',','.');
                    //decimal totalPedido = Convert.ToDecimal(totalPedidoString);
                    ////decimal prueba = decimal.Parse(totalPedidoString);
                    ////string Pedido = this.Request.Form["pedido"];        

                    //string condicionFlete = this.Request.Form["condicionFlete"];

                    //string descripcionEntrega = this.Request.Form["descripcionEntrega"];
                    //string descripcionRecepcion = this.Request.Form["descripcionRecepcion"];
                    //string numeroOrdenCompra = this.Request.Form["numeroOrden"];



                    //string Pedido = this.Request.Form["pedido"]; 
                    #endregion CodigoAnterior

                    string Pedido = this.Request.Form["data"];
                    dynamic jsonPedido = JsonConvert.DeserializeObject(Pedido);

                    int idEmpleado = jsonPedido.idEmpleado;
                    int idCliente = jsonPedido.idCliente;
                    DateTime fechaVencimiento = jsonPedido.fechaVencimiento;
                    string observaciones = jsonPedido.observaciones;
                    int? numeroPrecio = jsonPedido.numeroPrecio;
                    decimal totalPedido = jsonPedido.totalPedido;
                    string condicionFlete = jsonPedido.condicionFlete;
                    string descripcionEntrega = jsonPedido.descripcionEntrega;
                    string descripcionRecepcion = jsonPedido.descripcionRecepcion;
                    string numeroOrdenCompra = jsonPedido.numeroOrden;
                    string domicilioDeEntrega = jsonPedido.domicilioDeEntrega;
                    int? idClienteDomicilio = jsonPedido.idClienteDomicilio;

                    if (Global.ModoTraza)
                    {
                        Logger.Default.DebugFormat("    idEmpleado: {0}", idEmpleado);
                        Logger.Default.DebugFormat("    idCliente: {0}", idCliente);
                        Logger.Default.DebugFormat("    fechaVencimiento: {0}", fechaVencimiento);
                        Logger.Default.DebugFormat("    observaciones: {0}", observaciones);
                        Logger.Default.DebugFormat("    numeroPrecio: {0}", numeroPrecio);
                        Logger.Default.DebugFormat("    totalPedido: {0}", totalPedido);
                        Logger.Default.DebugFormat("    condicionFlete: {0}", condicionFlete);
                        Logger.Default.DebugFormat("    descripcionEntrega: {0}", descripcionEntrega);
                        Logger.Default.DebugFormat("    descripcionRecepcion: {0}", descripcionRecepcion);
                        Logger.Default.DebugFormat("    numeroOrden: {0}", numeroOrdenCompra);
                        Logger.Default.DebugFormat("    domicilioDeEntrega: {0}", domicilioDeEntrega);
                        Logger.Default.DebugFormat("    idClienteDomicilio: {0}", idClienteDomicilio);
                        Logger.Default.DebugFormat("    pedido: {0}", Pedido);
                    }
                    decimal totalRecalculado = 0;

                    Cliente cliente = HelperCliente.BuscarCliente(idCliente);
                    Talonario[] tal_list = OperacionesDocumento.BuscarTalonariosEnElPuesto("PED", false);
                    if (tal_list != null && tal_list.Length > 0)
                    {
                        DataTable tablaGrillaPedidos = OperacionesPedidos.CrearTablaGrillaPedidos();
                        //dynamic jsonPedido = JsonConvert.DeserializeObject(Pedido);                                                
                        foreach (var item in jsonPedido.pedido)
                        {
                            if (item.precioAproximado == true)
                            {
                                int idArticulo = item.idArticulo;
                                int cantidad = 1;
                                Articulo articulo = HelperArticulo.BuscarArticulo(idArticulo);
                                Venta ctxVenta = new Venta
                                {
                                    Cliente = cliente,
                                    ConfiguracionFacturacion = HelperConfiguracionFacturacion.BuscarConfiguracionFacturacion(),
                                    ConfiguracionCliente = HelperConfiguracionCliente.BuscarConfiguracionCliente()
                                };
                                DescuentosArticuloCalculados precioCalculado = Facturador.CalculoPrecio(articulo, cantidad, ctxVenta);
                                item.precio = precioCalculado.PrecioUnitario;
                                if (totalRecalculado == 0)
                                {
                                    totalRecalculado = this.RecalcularTotal(jsonPedido, cliente);
                                    totalPedido = totalRecalculado;
                                }
                            }

                            tablaGrillaPedidos.Rows.Add(new object[] { item.cantidad,
                                                                   item.codigoArticulo,
                                                                   item.descripcion,
                                                                   item.precio,
                                                                   numeroPrecio,
                                                                   item.descuento,
                                                                   "",                  //  InformacionAdicional
                                                                   totalPedido,
                                                                   null,                //  IdMoneda
                                                                   item.idArticulo,
                                                                   null,                //  IdPedidoDetalle
                                                                   null,                //  IdClienteDomicilio
                                                                   item.idDeposito      //  IdDepositoAImputar
                            });
                        }

                        int PuntoVenta = Convert.ToInt32(tal_list[0].PuntoVenta);
                        int ProximoNumero = Convert.ToInt32(tal_list[0].ProximoNumero);
                        int idEstacion = tal_list[0].IdEstacion;
                        int idPedido = OperacionesPedidos.PersisitirPedido(PuntoVenta,
                                                                           ProximoNumero,
                                                                           fechaVencimiento,
                                                                           cliente,
                                                                           observaciones,
                                                                           idEmpleado,
                                                                           numeroOrdenCompra,
                                                                           null,
                                                                           descripcionEntrega,
                                                                           descripcionRecepcion,
                                                                           idEstacion,
                                                                           totalPedido,
                                                                           condicionFlete,
                                                                           idClienteDomicilio,
                                                                           null,
                                                                           tablaGrillaPedidos);

                        string comprobante = Aoniken.CaldenOil.Common.StringHelper.Comprobante("PED", PuntoVenta, ProximoNumero);
                        tal_list[0].ProximoNumero = tal_list[0].ProximoNumero + 1;
                        OperacionesEntidades.PersistirEntidad(tal_list[0], OperacionesEntidades.InitialState.Updated);
                        return new Models.Respuesta
                        {
                            Valor = true,
                            Texto = $"Se cargo con éxito el pedido, comprobante {comprobante}",
                            Comprobante = comprobante,
                            Total = totalPedido
                        };
                    }
                    else
                    {
                        ContextoApp ctxAplicacion = ContextoApp.Instanciar();
                        string puesto = ctxAplicacion.Puesto.Nombre;

                        return new Models.Respuesta
                        {
                            Valor = false,
                            Texto = $"No hay un talonario definido para Pedidos en el puesto {puesto}"
                        };
                    }                   
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(Vemn.Framework.ExceptionManagement.ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    return new Models.Respuesta
                    {
                        Valor = false,
                        Texto = ex.Message.ToString()
                    };
                }
            }, null, name: "(uso interno)");

            Put<Models.Respuesta>("AnularPedido", p =>
            {
                try
                {
                    TextHelper.LoggearMetodo(p, this.Request, "AnularPedido", null);
                    this.RequiresAuthentication();
                    int idEmpleado = this.Request.Form["idEmpleado"];
                    int idCliente = this.Request.Form["idCliente"];
                    string comprobante = this.Request.Form["comprobante"];
                    
                    Cliente cliente = HelperCliente.BuscarCliente(idCliente);

                    List<Models.Pedido> listaPedidos = HelperSQL.GetListaPedidos(idEmpleado);
                    Models.Pedido pedidoParaAnular;
                    bool esAnulable = false;
                    string textoRespuesta = "No se pudo anular este pedido.";

                    foreach (var pedido in listaPedidos) 
                    {
                        if (pedido.Comprobante == comprobante){ 
                            pedidoParaAnular = pedido;
                            esAnulable = true;
                            textoRespuesta = $"Se anulo con éxito el pedido, comprobante {comprobante}";
                            OperacionesPedidos.AnularPedido(pedidoParaAnular.IdPedido);
                        }
                    }

                    Models.Respuesta respuesta = new Models.Respuesta
                    {
                        Valor = esAnulable,
                        Texto = textoRespuesta
                    };

                    return respuesta;
                }
                catch (Exception ex)
                {
                    return new Models.Respuesta
                    {
                        Valor = false,
                        Texto = ex.Message.ToString()
                    };
                }
            }, null, name: "(uso interno)");
        }
    }
}