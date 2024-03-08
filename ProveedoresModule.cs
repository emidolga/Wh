using Nancy;
using Nancy.Security;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using System.Collections.Generic;
using System;

namespace HostCaldenONNancy.Modules
{
    public class ProveedoresModule : NancyModule
    {
        public ProveedoresModule() : base("api/Proveedores/")
        {
            
            Get<Models.Proveedor[]>("GetAllProveedores", p =>
            {
                this.RequiresAuthentication();
                DateTime? ultimaFechaActualizacion = null;
                if (this.Request.Query["ultimaFechaActualizacion"].Value != null)
                {
                    ultimaFechaActualizacion = this.Request.Query["ultimaFechaActualizacion"];
                }
                List<Models.Proveedor> proveedoresLista = HelperSQL.GetAllProveedores(ultimaFechaActualizacion);                 
                return (proveedoresLista.ToArray());
            }, null, name: "Recupera la lista de proveedores ACTIVOS. Parámetros opcionales: {ultimaFechaActualizacion}");

            Get<Models.OrdenCompra[]>("GetOrdenesDeCompra", p =>
            {
                this.RequiresAuthentication();
                int idDeposito = this.Request.Query["idDeposito"];
                int idProveedor = this.Request.Query["idProveedor"];
                IList<OrdenCompra> ordenesCompra;
                ordenesCompra = HelperOrdenCompra.RecuperarOrdenCompraPendientesDeEntrega(idDeposito, idProveedor, false, false);
                
                List<Models.OrdenCompra> ordenesLista = new List<Models.OrdenCompra>();
                foreach (OrdenCompra orden in ordenesCompra){
                    ordenesLista.Add(new Models.OrdenCompra()
                    {
                        IdOrdenCompra = orden.IdOrdenCompra,
                        Descripcion = orden.DescripcionParaCombo
                    });
                }
                return (ordenesLista.ToArray());
            }, null, name: "Dado un depósito y un proveedor, recupera la lista de órdenes de compra pendientes de entrega. Parámetros: {idDeposito} {idProveedor}");

            Get<Models.InformacionCtaCteProveedor[]>("GetPagosARealizarProveedores", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionCtaCteProveedor> infoCtaCte = new List<Models.InformacionCtaCteProveedor>();
                try
                {
                    int? idProveedor = this.Request.Query["idProveedor"];
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];
                    infoCtaCte = HelperSQL.GetPagosARealizarProveedores(idProveedor ?? 0, hastaFecha ?? System.DateTime.Now);
                }
                catch
                { }
                return (infoCtaCte.ToArray());
            }, null, name: "Retorna una lista de los pagos a realizar al/los proveedor/es a filtrar. Parámetros: {idProveedor, si es 0 o vacío=todos} {hastaFecha, vacía=hoy}");

            Get<Models.InformacionSaldoProveedor[]>("GetSaldosCtaCteProveedores", p =>
            {
                this.RequiresAuthentication();
                List<Models.InformacionSaldoProveedor> saldos = new List<Models.InformacionSaldoProveedor>();
                try
                {
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];
                    saldos = HelperSQL.GetSaldosCtaCteProveedores(hastaFecha ?? System.DateTime.Now);
                }
                catch
                { }
                return (saldos.ToArray());
            }, null, name: "Retorna una lista de todos los saldos de los proveedores a la fecha solicitada. Parámetros: {hastaFecha, vacía=hoy}");

            Get<string>("GetFacturasCompraPendientes", p=>
            {
                this.RequiresAuthentication();
                string facturas = "";

                try
                {
                    System.DateTime? desdeFecha = this.Request.Query["desdeFecha"];
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];
                    string cuitProveedor = this.Request.Query["cuitProveedor"];

                    facturas = ServicioWebHost.FacturasCompraListener.GetFacturasPendientes(desdeFecha, hastaFecha, cuitProveedor);
                }
                catch (Exception ex)
                { 
                }
                return (facturas);
            }, null, name: "Retorna una lista de facturas de proveedores provista por Xpensify");

            Get<string>("GetFacturasCompraConErrores", p =>
            {
                this.RequiresAuthentication();
                string facturas = "";

                try
                {
                    System.DateTime? desdeFecha = this.Request.Query["desdeFecha"];
                    System.DateTime? hastaFecha = this.Request.Query["hastaFecha"];
                    string cuitProveedor = this.Request.Query["cuitProveedor"];

                    facturas = ServicioWebHost.FacturasCompraListener.GetFacturasConErrores(desdeFecha, hastaFecha, cuitProveedor);
                }
                catch
                { }
                return (facturas);
            }, null, name: "Retorna una lista de facturas de proveedores provista por Xpensify");

            Get<bool>("UpdateFacturaCompra", p =>
            {
                this.RequiresAuthentication();
                bool updateExitoso = false;

                try
                {
                    int idFacturaCompra = this.Request.Query["idFacturaCompra"];
                    updateExitoso = ServicioWebHost.FacturasCompraListener.UpdateFactura(idFacturaCompra);
                }
                catch
                {
                    updateExitoso = false;
                }

                return updateExitoso;
            }, null, name: "Actualiza el estado de la factura almacenada en Calden Oil en la API de Xpensify");

            Get<bool>("UpdateFacturaCompraConErrores", p =>
            {
                this.RequiresAuthentication();
                bool updateExitoso = false;

                try
                {
                    int idFacturaCompra = this.Request.Query["idFacturaCompra"];
                    string error = this.Request.Query["error"];
                    updateExitoso = ServicioWebHost.FacturasCompraListener.UpdateFacturaConErrores(idFacturaCompra, error);
                }
                catch
                {
                    updateExitoso = false;
                }

                return updateExitoso;
            }, null, name: "Actualiza el estado de la factura con errores en la API de Xpensify");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }

    }
}