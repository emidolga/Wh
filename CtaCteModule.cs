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

namespace HostCaldenONNancy.Modules
{
    public class CtaCteModule : NancyModule
    {
        public CtaCteModule() : base("api/CtaCte/")
        {
            Get<Models.ConfiguracionCtaCte>("GetConfiguracion", p =>
            {
                ConfiguracionCta configuracionCtaCte = HelperConfiguracionCta.BuscarConfiguracionCta();

                Models.ConfiguracionCtaCte config = new Models.ConfiguracionCtaCte
                {
                    ChequesNoAcreditadosAfectanLimiteCredito = configuracionCtaCte.ChequesNoAcreditadosAfectanLimiteCredito,
                    ChequesAFechaAfectanLimiteCredito = configuracionCtaCte.ChequesAFechaAfectanLimiteCredito,
                    RemitosNoFacturadosAfectanLimiteCredito = configuracionCtaCte.RemitosNoFacturadosAfectanLimiteCredito,
                    TipoPrecioCalculoRemitosNoFacturados = (int)configuracionCtaCte.TipoPrecioCalculoRemitosNoFacturados,
                    PermitirSobrepasarLimiteCreditoPorcentaje = configuracionCtaCte.PermitirSobrepasarLimiteCreditoPorcentaje,
                    PedidosNoRemitidosYNoFacturadosAfectanLimiteCredito = configuracionCtaCte.PedidosNoRemitidosYNoFacturadosAfectanLimiteCredito,
                    PercepcionesAfectanAlSaldo = configuracionCtaCte.PercepcionesAfectanAlSaldo,
                };

                return config;
            }, null, name: "(uso interno)");
            Get<Models.Recibos[]>("GetRecibosEntreFechas", p =>
            {
                this.RequiresAuthentication();
                List<Models.Recibos> recibosLista = null;
                try
                {
                    string desdeFecha = Request.Query["desdeFecha"];
                    string hastaFecha = Request.Query["hastaFecha"];
                    if (desdeFecha != "" && hastaFecha != "")
                    {
                        recibosLista = HelperSQL.GetListaRecibosEntreFechas(desdeFecha, hastaFecha);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (recibosLista.ToArray());
            }, null, name: "Devuelve la lista de recibos entre dos fechas dadas. Parámetros: {desdeFecha, hastaFecha}");

            Get<Models.Recibos[]>("GetRecibosPorComprobante", p =>
            {
                this.RequiresAuthentication();
                List<Models.Recibos> recibosLista = null;
                try
                {
                    string idTipoMovimiento = Request.Query["tipoMovimiento"];
                    int puntoVenta = Request.Query["puntoVenta"];
                    int numero = Request.Query["numero"];
                    if (idTipoMovimiento != "" && puntoVenta > 0 && numero > 0)
                    {
                        recibosLista = HelperSQL.GetListaRecibosPorComprobante(idTipoMovimiento, puntoVenta, numero);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (recibosLista.ToArray());
            }, null, name: "Devuelve la lista de recibos dado un comprobante. Parámetros: {tipoMovimiento, puntoVenta, numero}");
        }
    }
}
