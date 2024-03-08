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
using Wilson.ORMapper;
using System.Linq;

namespace HostCaldenONNancy.Modules
{
    public class FacturasVentaTransporteModule : NancyModule
    {
        private void CargarComprobanteEnVenta(Venta ctxVenta, Models.FacturasVentaTransporte fac)
        {
            ctxVenta.Comprobantes[0].MovimientoFac.IdTipoMovimiento = "FAA";
            ctxVenta.Comprobantes[0].MovimientoFac.PuntoVenta = fac.cabecera.PuntoVenta;
            ctxVenta.Comprobantes[0].MovimientoFac.Numero = fac.cabecera.Numero;
            ctxVenta.Comprobantes[0].MovimientoFac.Fecha = DateTime.Now; //fac.cabecera.Fecha;
            ctxVenta.Comprobantes[0].MovimientoFac.FechaEmision = DateTime.Now;
            ctxVenta.Comprobantes[0].MovimientoFac.IdCliente = ctxVenta.Cliente.IdCliente;
            ctxVenta.Comprobantes[0].MovimientoFac.RazonSocial = ctxVenta.Cliente.RazonSocial;
            ctxVenta.Comprobantes[0].MovimientoFac.IdCategoriaIVA = ctxVenta.Cliente.IdCategoriaIVA;
            ctxVenta.Comprobantes[0].MovimientoFac.IdTipoDocumento = ctxVenta.Cliente.IdTipoDocumento;
            ctxVenta.Comprobantes[0].MovimientoFac.NumeroDocumento = ctxVenta.Cliente.NumeroDocumento;
            ctxVenta.Comprobantes[0].MovimientoFac.Domicilio = ctxVenta.Cliente.DomicilioCompleto;
            ctxVenta.Comprobantes[0].MovimientoFac.IdLocalidad = HelperLocalidad.BuscarLocalidad(fac.cabecera.Localidad, fac.cabecera.CodigoPostal.ToString()).IdLocalidad;
            ctxVenta.Comprobantes[0].MovimientoFac.IdCondicionVenta = ctxVenta.Cliente.IdCondicionVenta;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoNoGravado = fac.cabecera.NetoNoGravado;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoMercaderias = fac.cabecera.NetoGravado;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoCombustibles = 0;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoLubricantes = 0;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoCigarrillos = 0;
            ctxVenta.Comprobantes[0].MovimientoFac.NetoConceptosFinancieros = 0;
            ctxVenta.Comprobantes[0].MovimientoFac.IVA = fac.cabecera.IVA;
            ctxVenta.Comprobantes[0].MovimientoFac.ImpuestoInterno = fac.cabecera.ImpuestoInterno;
            ctxVenta.Comprobantes[0].MovimientoFac.Tasas = fac.cabecera.Tasas;
            ctxVenta.Comprobantes[0].MovimientoFac.PercepcionIIBB = fac.cabecera.PercepcionIIBB;
            ctxVenta.Comprobantes[0].MovimientoFac.PercepcionIVA = fac.cabecera.PercepcionIVA;
            ctxVenta.Comprobantes[0].MovimientoFac.OtrasPercepciones = fac.cabecera.OtrasPercepciones;
            ctxVenta.Comprobantes[0].MovimientoFac.Total = fac.cabecera.Total;
            ctxVenta.Comprobantes[0].MovimientoFac.Consignado = true;
            ctxVenta.Comprobantes[0].MovimientoFac.DocumentoFiscal = false;
            ctxVenta.Comprobantes[0].MovimientoFac.DocumentoAnticipado = false;
            ctxVenta.Comprobantes[0].MovimientoFac.IdPuesto = 3; //VER QUE PONER
            ctxVenta.Comprobantes[0].MovimientoFac.DocumentoCancelado = false;
            ctxVenta.Comprobantes[0].MovimientoFac.IdEstacion = 1; //VER QUE PONER
            ctxVenta.Comprobantes[0].MovimientoFac.DocumentoConsumoInterno = false;
            ctxVenta.Comprobantes[0].MovimientoFac.IdEmpleadoResponsable = 1; //VER QUE PONER
            ctxVenta.Comprobantes[0].MovimientoFac.TasaVial = fac.cabecera.TasaVial;

            foreach (Models.Detalle detalle in fac.detalle)
            {
                MovimientoDetalleFac movDetalleFac = Global.Data.GetObject<MovimientoDetalleFac>();
                movDetalleFac.IdArticulo = HelperArticulo.BuscarArticuloPorDescripcion(detalle.DescripcionArticulo, "").IdArticulo;
                movDetalleFac.Cantidad = detalle.Cantidad;
                movDetalleFac.Precio = detalle.Precio;
                movDetalleFac.IVA = detalle.IvaUnitario;
                movDetalleFac.ImpuestoInterno = detalle.ImpuestoInternoUnitario;
                movDetalleFac.Tasas = detalle.TasasUnitario;
                movDetalleFac.Costo = detalle.CostoUnitario;
                movDetalleFac.Facturado = true;
                movDetalleFac.TasaVial = detalle.TasaVialUnitario;

                ctxVenta.Comprobantes[0].MovimientoFac.MovimientosDetalleFacList.Add(movDetalleFac);
            }

            MovimientoFacPropiedades movFacPropiedades = new MovimientoFacPropiedades();
            movFacPropiedades.CAE = fac.propiedades.Cae;
            movFacPropiedades.FacturaElectronica = fac.propiedades.FacturaElectronica;
            movFacPropiedades.VencimientoCAE = DateTime.Now; //CORREGIR

            ctxVenta.Comprobantes[0].MovimientoFac.MovimientoFacPropiedadesList.Add(movFacPropiedades);
        }

        private void PersistirFacturaTransporte(Models.FacturasVentaTransporte fac, Cliente cliente, string idTipoMovimiento)
        {
            Transaction tx = null;

            ConfiguracionFacturacion configFacturacion = HelperConfiguracionFacturacion.BuscarConfiguracionFacturacion();
            ConfiguracionContabilidad configContabilidad = HelperConfiguracionContabilidad.BuscarConfiguracionContabilidad();

            MovimientoFac movFac = new MovimientoFac();

            movFac.IdTipoMovimiento = idTipoMovimiento;
            movFac.PuntoVenta = fac.cabecera.PuntoVenta;
            movFac.Numero = fac.cabecera.Numero;
            movFac.Fecha = Convert.ToDateTime(fac.cabecera.Fecha);
            movFac.FechaEmision = Convert.ToDateTime(fac.cabecera.Fecha);
            movFac.IdCliente = cliente.IdCliente;
            movFac.RazonSocial = cliente.RazonSocial;
            movFac.IdCategoriaIVA = cliente.IdCategoriaIVA;
            movFac.IdTipoDocumento = cliente.IdTipoDocumento;
            movFac.NumeroDocumento = cliente.NumeroDocumento;
            movFac.Domicilio = cliente.DomicilioCompleto;
            movFac.IdLocalidad = HelperLocalidad.BuscarLocalidad(fac.cabecera.Localidad, fac.cabecera.CodigoPostal.ToString()).IdLocalidad;
            movFac.IdCondicionVenta = cliente.IdCondicionVenta;
            movFac.Patente = fac.cabecera.Patente;
            movFac.NetoNoGravado = fac.cabecera.NetoNoGravado;
            movFac.NetoMercaderias = 0;
            movFac.NetoCigarrillos = 0;
            movFac.NetoCombustibles = fac.cabecera.NetoGravado;
            movFac.NetoLubricantes = 0;
            movFac.NetoConceptosFinancieros = 0;
            movFac.IVA = fac.cabecera.IVA;
            movFac.ImpuestoInterno = fac.cabecera.ImpuestoInterno;
            movFac.Tasas = fac.cabecera.Tasas;
            movFac.PercepcionIIBB = fac.cabecera.PercepcionIIBB;
            movFac.PercepcionIVA = fac.cabecera.PercepcionIVA;
            movFac.OtrasPercepciones = fac.cabecera.OtrasPercepciones;
            movFac.Total = fac.cabecera.Total;
            movFac.Consignado = true;
            movFac.DocumentoFiscal = false;
            movFac.DocumentoAnticipado = false;
            if (configFacturacion.IdPuestoPorDefectoFacturasTransporte != null)
            {
                movFac.IdPuesto = configFacturacion.IdPuestoPorDefectoFacturasTransporte;
            } else
            {
                List<Puesto> puestos = HelperPuesto.RecuperarTodosLosPuestos();
                movFac.IdPuesto = puestos.FirstOrDefault().IdPuesto;
            }
            movFac.Transaccion = Guid.NewGuid();
            movFac.UserName = "WebHost/FacturasTransporte";
            movFac.DocumentoCancelado = false;
            //movFac.IdEstacion = 1; //VER QUE PONER
            movFac.DocumentoConsumoInterno = false;
            Empleado empleado = HelperEmpleado.BuscarEmpleadoPorTag("IntegracionFacturasCompra");
            if (empleado != null)
            {
                movFac.IdEmpleadoResponsable = empleado.IdEmpleado;
            } else
            {
                movFac.IdEmpleadoResponsable = null;
            }
            
            movFac.TasaVial = fac.cabecera.TasaVial;

            List<MovimientoDetalleFac> movDetalleFacList = new List<MovimientoDetalleFac>();
            foreach (Models.Detalle detalle in fac.detalle)
            {
                MovimientoDetalleFac movDetalleFac = Global.Data.GetObject<MovimientoDetalleFac>();
                movDetalleFac.IdArticulo = HelperArticulo.BuscarArticuloPorDescripcion(detalle.DescripcionArticulo, "").IdArticulo;
                movDetalleFac.Cantidad = detalle.Cantidad;
                movDetalleFac.Precio = detalle.Precio;
                movDetalleFac.IVA = detalle.IvaUnitario;
                movDetalleFac.ImpuestoInterno = detalle.ImpuestoInternoUnitario;
                movDetalleFac.Tasas = detalle.TasasUnitario;
                movDetalleFac.Costo = detalle.CostoUnitario;
                movDetalleFac.Facturado = true;
                movDetalleFac.UserName = "WebHost/FacturasTransporte";
                movDetalleFac.TasaVial = detalle.TasaVialUnitario;
                movDetalleFacList.Add(movDetalleFac);
            }

            MovimientoFacPropiedades movFacPropiedades = new MovimientoFacPropiedades();
            movFacPropiedades.UserName = "WebHost/FacturasTransporte";
            movFacPropiedades.CAE = fac.propiedades.Cae;
            movFacPropiedades.FacturaElectronica = fac.propiedades.FacturaElectronica;
            movFacPropiedades.VencimientoCAE = Convert.ToDateTime(fac.cabecera.Fecha);

            MovimientoCta movCta = Global.Data.GetObject<MovimientoCta>();
            movCta.IdCliente = cliente.IdCliente;
            movCta.Fecha = Convert.ToDateTime(fac.cabecera.Fecha);
            movCta.FechaVencimiento = Convert.ToDateTime(fac.cabecera.Fecha);
            movCta.IdTipoMovimiento = idTipoMovimiento;
            movCta.Importe = (decimal)movFac.Total;
            movCta.Transaccion = Guid.NewGuid();
            movCta.UserName = "WebHost/FacturasTransporte";

            try
            {
                using (tx = Global.Data.BeginTransaction())
                {
                    OperacionesEntidades.PersistirEntidadInsercion(movFac, tx);
                    foreach (MovimientoDetalleFac movDetalleFac in movDetalleFacList)
                    {
                        movDetalleFac.IdMovimientoFac = movFac.IdMovimientoFac;
                        OperacionesEntidades.PersistirEntidadInsercion(movDetalleFac, tx);
                    }
                    movFacPropiedades.IdMovimientoFac = movFac.IdMovimientoFac;
                    OperacionesEntidades.PersistirEntidadInsercion(movFacPropiedades, tx);
                    movCta.IdMovimientoFac = movFac.IdMovimientoFac;
                    OperacionesEntidades.PersistirEntidadInsercion(movCta, tx);
                    movCta.IdMovimientoImputado = movCta.IdMovimientoCta;
                    OperacionesEntidades.PersistirEntidadActualizacion(movCta, tx);
                    tx.Commit();
                } //using

                try
                {
                    if (configContabilidad.GeneraAsientos)
                    {
                        using (tx = Global.Data.BeginTransaction())
                        {
                            Comprobante comp = new Comprobante();
                            comp.MovimientoFac = movFac;
                            Venta venta = new Venta();
                            venta.Comprobantes.Add(comp);
                            Aoniken.CaldenOil.ReglasNegocio.Facturador.ValidarComprobante(venta, idTipoMovimiento);

                            string mensajeError = "";
                            if (OperacionesContabilidad.GenerarAsientosFacturacion(comp, tx, out mensajeError))
                            {
                                tx.Commit();
                            }
                        }
                    }
                }
                catch
                {
                    //SIGUE
                }
            }
            catch (Exception ex)
            {
                OperacionesEntidades.RollbackTransaction(tx);
                throw;
            
            }
        }

        private string SetearIdTipoComprobante(string tipoComprobante, string letraComprobante)
        {
            string idTipoMovimiento = "";
            if (tipoComprobante.ToLower() == "factura")
            {
                switch (letraComprobante.ToLower())
                {
                    case "a": idTipoMovimiento = "FAA";
                    break;
                    case "b": idTipoMovimiento = "FAB";
                    break;
                    case "c": idTipoMovimiento = "FAC";
                    break;
                }
            } else if (tipoComprobante.ToLower() == "nota de credito")
            {
                switch (letraComprobante.ToLower())
                {
                    case "a":
                        idTipoMovimiento = "NCA";
                        break;
                    case "b":
                        idTipoMovimiento = "NCB";
                        break;
                    case "c":
                        idTipoMovimiento = "NCC";
                        break;
                }
            } else if (tipoComprobante.ToLower() == "nota de debito")
            {
                switch (letraComprobante.ToLower())
                {
                    case "a":
                        idTipoMovimiento = "NDA";
                        break;
                    case "b":
                        idTipoMovimiento = "NDB";
                        break;
                    case "c":
                        idTipoMovimiento = "NDC";
                        break;
                }
            }

            return idTipoMovimiento;
        }

        public FacturasVentaTransporteModule() : base("api/FacturasVentaTransporte/")
        {
            Put<Models.Respuesta>("PersistirFacturaVentaTransporte", p =>
            {
                try
                {
                    this.RequiresAuthentication();
                    string jsonFacturas = this.Request.Query["facturas"];

                    //List<Models.FacturasVentaTransporte> facturas = new List<Models.FacturasVentaTransporte>();

                    //Models.FacturasVentaTransporte factura = new Models.FacturasVentaTransporte();

                    //factura.cabecera = new Models.Cabecera();
                    //factura.cabecera.TipoComprobante = "Factura";
                    //factura.cabecera.LetraComprobante = "A";
                    //factura.cabecera.PuntoVenta = 10;
                    //factura.cabecera.Numero = 1;
                    //factura.cabecera.Fecha = "02/03/2023";
                    //factura.cabecera.RazonSocial = "Prueba Cliente";
                    //factura.cabecera.NumeroDocumento = "30-70797094-0";
                    //factura.cabecera.Domicilio = "Calle 123";
                    //factura.cabecera.Localidad = "Bahia Blanca";
                    //factura.cabecera.CodigoPostal = 8000;
                    //factura.cabecera.NetoNoGravado = 0;
                    //factura.cabecera.NetoGravado = 5000;
                    //factura.cabecera.IVA = 1050;
                    //factura.cabecera.ImpuestoInterno = 0;
                    //factura.cabecera.Tasas = 0;
                    //factura.cabecera.TasaVial = 0;
                    //factura.cabecera.PercepcionIIBB = 0;
                    //factura.cabecera.PercepcionIVA = 0;
                    //factura.cabecera.OtrasPercepciones = 0;
                    //factura.cabecera.Total = 0;

                    //factura.detalle = new List<Models.Detalle>();
                    //Models.Detalle detalleFactura = new Models.Detalle();
                    //detalleFactura.Cantidad = 1;
                    //detalleFactura.CodigoArticulo = "1464";
                    //detalleFactura.DescripcionArticulo = "* ACEITE GIRASOL BIDON 5 LT";
                    //detalleFactura.Precio = 1000;
                    //detalleFactura.IvaUnitario = 210;
                    //detalleFactura.ImpuestoInternoUnitario = 0;
                    //detalleFactura.TasasUnitario = 0;
                    //detalleFactura.TasaVialUnitario = 0;
                    //detalleFactura.CostoUnitario = 0;
                    //factura.detalle.Add(detalleFactura);

                    //detalleFactura = new Models.Detalle();
                    //detalleFactura.Cantidad = 2;
                    //detalleFactura.CodigoArticulo = "1524";
                    //detalleFactura.DescripcionArticulo = "Azucar Changuito 500 Gr";
                    //detalleFactura.Precio = 2000;
                    //detalleFactura.IvaUnitario = 840;
                    //detalleFactura.ImpuestoInternoUnitario = 0;
                    //detalleFactura.TasasUnitario = 0;
                    //detalleFactura.TasaVialUnitario = 0;
                    //detalleFactura.CostoUnitario = 0;
                    //factura.detalle.Add(detalleFactura);

                    //factura.propiedades = new Models.Propiedades();
                    //factura.propiedades.Cae = "72055834554811";
                    //factura.propiedades.FacturaElectronica = true;
                    //factura.propiedades.VencimientoCae = "02/03/2023";

                    //facturas.Add(factura);

                    //factura = new Models.FacturasVentaTransporte();

                    //factura.cabecera = new Models.Cabecera();
                    //factura.cabecera.TipoComprobante = "Factura";
                    //factura.cabecera.LetraComprobante = "A";
                    //factura.cabecera.PuntoVenta = 10;
                    //factura.cabecera.Numero = 2;
                    //factura.cabecera.Fecha = "02/03/2023";
                    //factura.cabecera.RazonSocial = "Prueba Cliente";
                    //factura.cabecera.NumeroDocumento = "30-70797094-0";
                    //factura.cabecera.Domicilio = "Calle 123";
                    //factura.cabecera.Localidad = "Bahia Blanca";
                    //factura.cabecera.CodigoPostal = 8000;
                    //factura.cabecera.NetoNoGravado = 0;
                    //factura.cabecera.NetoGravado = 5000;
                    //factura.cabecera.IVA = 1050;
                    //factura.cabecera.ImpuestoInterno = 0;
                    //factura.cabecera.Tasas = 0;
                    //factura.cabecera.TasaVial = 0;
                    //factura.cabecera.PercepcionIIBB = 0;
                    //factura.cabecera.PercepcionIVA = 0;
                    //factura.cabecera.OtrasPercepciones = 0;
                    //factura.cabecera.Total = 0;

                    //factura.detalle = new List<Models.Detalle>();
                    //detalleFactura = new Models.Detalle();
                    //detalleFactura.Cantidad = 1;
                    //detalleFactura.CodigoArticulo = "1464";
                    //detalleFactura.DescripcionArticulo = "* ACEITE GIRASOL BIDON 5 LT";
                    //detalleFactura.Precio = 1000;
                    //detalleFactura.IvaUnitario = 210;
                    //detalleFactura.ImpuestoInternoUnitario = 0;
                    //detalleFactura.TasasUnitario = 0;
                    //detalleFactura.TasaVialUnitario = 0;
                    //detalleFactura.CostoUnitario = 0;
                    //factura.detalle.Add(detalleFactura);

                    //detalleFactura = new Models.Detalle();
                    //detalleFactura.Cantidad = 2;
                    //detalleFactura.CodigoArticulo = "1524";
                    //detalleFactura.DescripcionArticulo = "Azucar Changuito 500 Gr";
                    //detalleFactura.Precio = 2000;
                    //detalleFactura.IvaUnitario = 840;
                    //detalleFactura.ImpuestoInternoUnitario = 0;
                    //detalleFactura.TasasUnitario = 0;
                    //detalleFactura.TasaVialUnitario = 0;
                    //detalleFactura.CostoUnitario = 0;
                    //factura.detalle.Add(detalleFactura);

                    //factura.propiedades = new Models.Propiedades();
                    //factura.propiedades.Cae = "72055834554811";
                    //factura.propiedades.FacturaElectronica = true;
                    //factura.propiedades.VencimientoCae = "02/03/2023";

                    //facturas.Add(factura);

                    //string json = JsonConvert.SerializeObject(facturas);

                    List<Models.FacturasVentaTransporte> facturasFinal = JsonConvert.DeserializeObject<List<Models.FacturasVentaTransporte>>(jsonFacturas);

                    int cantidadFacturasAlmacenadas = 0;
                    int cantidadFacturasNoAlmacenadas = 0;

                    foreach (Models.FacturasVentaTransporte fac in facturasFinal)
                    {
                        Cliente cliente = HelperCliente.BuscarClientePorNumeroDocumento(fac.cabecera.NumeroDocumento, out string razonSocial);
                        string idTipoComprobante = SetearIdTipoComprobante(fac.cabecera.TipoComprobante, fac.cabecera.LetraComprobante);
                        if (!HelperMovimientoFac.ExisteComprobanteCliente(cliente.IdCliente, idTipoComprobante, fac.cabecera.PuntoVenta, fac.cabecera.Numero))
                        {
                            PersistirFacturaTransporte(fac, cliente, idTipoComprobante);
                            cantidadFacturasAlmacenadas++;
                        } else
                        {
                            cantidadFacturasNoAlmacenadas++;
                        }
                    }

                    if (cantidadFacturasAlmacenadas > 0 && cantidadFacturasNoAlmacenadas > 0)
                    {
                        return new Models.Respuesta
                        {
                            Valor = true,
                            Texto = $"Se cargaron con éxito {cantidadFacturasAlmacenadas} facturas de transporte. Otras {cantidadFacturasNoAlmacenadas} facturas de transporte no fueron cargadas porque ya existen en la base de datos",
                        };
                    } else if (cantidadFacturasAlmacenadas > 0 && cantidadFacturasNoAlmacenadas == 0)
                    {
                        return new Models.Respuesta
                        {
                            Valor = true,
                            Texto = $"Se cargaron con éxito {cantidadFacturasAlmacenadas} facturas de transporte.",
                        };
                    } else if (cantidadFacturasAlmacenadas == 0 && cantidadFacturasNoAlmacenadas > 0)
                    {
                        return new Models.Respuesta
                        {
                            Valor = true,
                            Texto = $"No se han cargado {cantidadFacturasNoAlmacenadas} facturas de transporte porque ya existen en la base de datos",
                        };
                    } else
                    {
                        return new Models.Respuesta
                        {
                            Valor = true,
                            Texto = $"No se ha cargado ninguna factura de transporte.",
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new Models.Respuesta
                    {
                        Valor = false,
                        Texto = ex.Message.ToString()
                    };
                }
            }, null, name: "Inserta en Calden Oil las facturas de transporte, indicadas mediante un JSON. Parámetros: {JsonFacturas}");
        }
    }
}