using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vemn.Framework.Logging;
using Aoniken.CaldenOil.Common;
using Aoniken.CaldenOil.Helpers;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.ReglasNegocio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HostCaldenONNancy.Modules
{
    public class InventarioModule : NancyModule
    {
        public InventarioModule() : base("api/Inventario/")
        {
            Get<Models.ConfiguracionInventario>("GetConfiguracion", p =>
            {
                ConfiguracionStock configuracionStock = HelperConfiguracionStock.BuscarConfiguracionStock();
                string msgError = "";
                TipoControlStock TipoControl;
                string TipoControlDescripcion = "";

                switch (configuracionStock.TipoControlStockMovil)
                {
                    case TipoControlStock.AlAzar:
                        TipoControl = TipoControlStock.AlAzar;
                        TipoControlDescripcion = "al azar";
                        if (configuracionStock.CantidadArticulosControlAlAzar.Equals(0))
                        {
                            msgError = "Debe indicar en la configuración de stock la cantidad de artículos a controlar.";
                        }
                        break;
                    case TipoControlStock.Selectivo:
                        TipoControl = TipoControlStock.Selectivo;
                        TipoControlDescripcion = "selectivo";
                        if (configuracionStock.CantidadArticulosControlAlAzar.Equals(0))
                        {
                            msgError = "Debe indicar en la configuración de stock la cantidad de artículos a controlar.";
                        }
                        else if ((configuracionStock.IdGrupoArticuloControlMovil1 == null) && (configuracionStock.IdGrupoArticuloControlMovil2 == null) && (configuracionStock.IdGrupoArticuloControlMovil3 == null))
                        {
                            msgError = "Debe indicar en la configuración de stock los grupos sobre los cuales desea hacer el control.";
                        }
                        break;
                    case TipoControlStock.Voluntario:
                        TipoControl = TipoControlStock.Voluntario;
                        TipoControlDescripcion = "voluntario";
                        break;
                }


                Models.ConfiguracionInventario config = new Models.ConfiguracionInventario
                {
                    TipoControl = configuracionStock.TipoControlStockMovil.ToString(),
                    TipoControlDescripcion = TipoControlDescripcion,
                    CantidadArticulosControlAlAzar = configuracionStock.CantidadArticulosControlAlAzar,
                    MostrarStockTeorico = configuracionStock.MostrarStockTeoricoEnControlStockMovil,
                    BuscarEnSinonimos = configuracionStock.BuscarSinonimosEnStockMovil,
                    BuscarEnCodigo = configuracionStock.BuscarPorCodigoEnStockMovil,
                    MsgError = msgError
                };

                return config;
            }, null, name: "(uso interno)");

            Get<Models.ArticuloIMA[]>("GetListaArticulosIMA", p =>
            {
                int idDeposito = this.Request.Query["idDeposito"];

                //this.RequiresAuthentication();
                List<Models.ArticuloIMA> articulosLista = HelperSQL.GetListaArticulosIMA(idDeposito);
                return (articulosLista.ToArray());
            }, null, name: "Dado un depósito, retorna la lista de artículos marcados para Control de Stock Móvil (IMA). Parámetros: {idDeposito}");

            Get<Models.MovimientoStock>("GetMovimientoStock", p =>
            {
                int idArticulo = this.Request.Query["idArticulo"];
                int idDeposito = this.Request.Query["idDeposito"];

                string query = String.Format("IdArticulo = '{0}' AND IdDeposito = '{1}'", idArticulo, idDeposito);
                MovimientoStock movStock = HelperEntidades.GetEntity<MovimientoStock>(query);
                
                if (movStock != null)
                {
                    Models.MovimientoStock movimientoStock = new Models.MovimientoStock
                    {
                        IdArticulo = movStock.IdArticulo,
                        IdDeposito = movStock.IdDeposito,
                        Cantidad = movStock.Cantidad,
                        IdTipoMovimiento = movStock.IdTipoMovimiento
                    };
                    return movimientoStock;
                }
                else
                {
                    return null;
                }
            }, null, name: "(uso interno)");

            Get<Models.ArticuloIMA>("GetArticuloIMAPorCodigoDeBarras", p =>
            {
                string codBarra = this.Request.Query["codBarra"];

                Logger.Default.Debug("Codigo de barra: " + codBarra);
                //int idDeposito = this.Request.Query["idDeposito"];
                ConfiguracionStock configuracionStock = HelperConfiguracionStock.BuscarConfiguracionStock();

                bool buscarEnSinonimos = configuracionStock.BuscarSinonimosEnStockMovil;
                bool buscarEnCodigo = configuracionStock.BuscarPorCodigoEnStockMovil;
                
                string msgError = "";
                
                Articulo articulo;
                try
                {
                    articulo = OperacionesArticulo.BuscarArticuloPorCodigoDeBarrasOCodigoDeArticulo(codBarra, buscarEnSinonimos, buscarEnCodigo, out string output);
                
                    if (articulo == null)
                    {
                        Models.ArticuloIMA articuloIMAVacio = new Models.ArticuloIMA
                        {
                            IdArticulo = null,
                            Descripcion = null,
                            MsgError = output
                        };

                        return articuloIMAVacio;
                        
                    }
                    else
                    {
                        if (articulo.GruposArticuloFK.Combustible)
                        {
                            msgError = "El artículo ingresado no es apto para control de stock";
                        }                    
                    }
                

                    Models.ArticuloIMA articuloIMA = new Models.ArticuloIMA
                    {
                        IdArticulo = articulo.IdArticulo,
                        Descripcion = articulo.Descripcion,
                        MsgError = msgError
                    };

                    return articuloIMA;
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ex.ToString());
                    return null;
                }
            }, null, name: "Dado un código de barras, retorna el artículo asociado si es que es apto para Control de Stock Móvil (IMA). Parámetros: {codBarra}");

            Get<Models.ArticuloIMA>("GetArticuloIMAPorId", p =>
            {
                int idArticulo = this.Request.Query["idArticulo"];                

                Articulo articulo;

                articulo = HelperArticulo.BuscarArticulo(idArticulo);

                Models.ArticuloIMA articuloIMA = new Models.ArticuloIMA
                {
                    Codigo = articulo.Codigo,
                    CodigoBarra = articulo.CodigoBarra,
                    IdArticulo = articulo.IdArticulo,
                    Descripcion = articulo.Descripcion,
                    Ubicacion = articulo.Ubicacion,
                    
                };

                return articuloIMA;
            }, null, name: "Dado su identificador, retorna el detalle de un artículo habilitado para Control de Stock Móvil (IMA). Parámetros: {idArticulo}");

            Get<Models.StockTeoricoIMA>("GetStockTeoricoIMA", p =>
            {
                int idArticulo = this.Request.Query["idArticulo"];
                int idDeposito = this.Request.Query["idDeposito"];
                //System.DateTime? fecha = this.Request.Query["fecha"];

                DateTime fecha = HelperFechas.FechaHoraActual();

                decimal stockTeorico = OperacionesStock.ConsultarSaldoStock(idArticulo, fecha, idDeposito);

                Models.StockTeoricoIMA stockTeoricoIMA = new Models.StockTeoricoIMA
                {
                    StockTeorico = stockTeorico
                };

                return stockTeoricoIMA;
            }, null, name: "Dado su artículo y un depósito, retorna el saldo de stock teórico al día de hoy. Parámetros: {idArticulo} {idDeposito}");


            Post<string>("PersistirAjustes", p =>
            {
                try
                {
                    int idEmpleado = this.Request.Form["idEmpleado"];
                    int idDeposito = this.Request.Form["idDeposito"];
                    string Movimientos = this.Request.Form["movimientos"];

                    IList<MovimientoStock> movimientosPorAjuste = new List<MovimientoStock>();
                    List<Articulo> noAjustados = new List<Articulo>();

                    int idOtroMovimientoStockAJE = 0;
                    bool GenerarCabeceraAJE = true;
                    int idOtroMovimientoStockAJI = 0;
                    bool GenerarCabeceraAJI = true;

                    dynamic jsonMovimientos = JsonConvert.DeserializeObject(Movimientos);
                    int stockReal;
                    int stockTeorico;
                    int ajuste; // Stock Real - Stock Teorico
                    int idArticulo;
                    foreach (var mov in jsonMovimientos)
                    {
                        stockReal = mov["stockReal"];
                        stockTeorico = mov["stockTeorico"];
                        ajuste = stockReal - stockTeorico;
                        if (ajuste != 0)
                        {
                            idArticulo = mov["idArticulo"];
                            MovimientoStock movStock = Global.Data.GetObject<MovimientoStock>();
                            movStock.IdArticulo = idArticulo;
                            movStock.Cantidad = Math.Abs(ajuste);
                            movStock.IdDeposito = idDeposito;
                            movStock.IdTipoMovimiento = ajuste > 0 ? "AJI" : "AJE";

                            movimientosPorAjuste.Add(movStock);
                        }
                        else
                        {
                            idArticulo = mov["idArticulo"];
                            Articulo articuloNoAjustado = HelperArticulo.BuscarArticulo(idArticulo);
                            noAjustados.Add(articuloNoAjustado);
                        }
                        
                    }

                    NroComprobante nroComprobante = new NroComprobante();

                    OperacionesStock.PersistirAjusteMovil(
                                     ref idOtroMovimientoStockAJE, ref GenerarCabeceraAJE, ref idOtroMovimientoStockAJI, ref GenerarCabeceraAJI,
                                     movimientosPorAjuste, noAjustados, idEmpleado, idDeposito, ref nroComprobante);

                    return nroComprobante.ToString();
                }
                catch(Exception ex)
                {
                    return ex.Message.ToString();
                }
            }, null, name: "(uso interno)");
        }
    }
}