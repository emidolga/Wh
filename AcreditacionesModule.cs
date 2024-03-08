using Nancy;
using Nancy.Security;
using System;
using System.Data;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.Helpers;
using Vemn.Framework.ExceptionManagement;
using Vemn.Framework.Logging;
using System.Linq;
using Aoniken.CaldenOil.Common;

namespace HostCaldenONNancy.Modules
{
    public class AcreditacionesModule : NancyModule
    {
        
        public AcreditacionesModule() : base("api/Acreditaciones")
        {

            Get<Models.AcreditacionCalendario[]>("GetCalendario", p =>
            {
                this.RequiresAuthentication();
                List<Models.AcreditacionCalendario> listaAcreditaciones = new List<Models.AcreditacionCalendario>();
                try
                {
                    DateTime fechaDesde = Request.Query["fechaDesde"];
                    DateTime fechaHasta = Request.Query["fechaHasta"];
                    int? idCuentaBancaria = Request.Query["idCuentaBancaria"];
                    DataTable tablaPercepcionesYRetenciones = OperacionesTarjetasCredito.GenerarTablaPercepcionesYRetenciones();
                    string queryFiltroTarjetas = "(Activa = 1 OR PagoElectronico = 1)";
                    if (idCuentaBancaria != null)
                    {
                        queryFiltroTarjetas = queryFiltroTarjetas + String.Format($" AND IdCuentaBancaria_Acreditacion = {idCuentaBancaria}");
                    }
                    Logger.Default.Info(String.Format("Recuperando tarjetas con la cláusula: {0}", queryFiltroTarjetas));
                    List<TarjetaCredito> listaTarjetas = HelperEntidades.GetEntityList<TarjetaCredito>(queryFiltroTarjetas, "").ToList<TarjetaCredito>();
                    TimeSpan ts = fechaHasta - fechaDesde;
                    int cantidadDias = ts.Days;
                    Logger.Default.Info(String.Format("Existen {2} días de diferencia entre la fecha {0} y {1}", fechaDesde, fechaHasta, cantidadDias));
                    for (int i = 0; i <= cantidadDias; i++)
                    {
                        DateTime diaCalendario = fechaDesde.AddDays(i);
                        decimal totalAcreditado = 0;
                        decimal totalPresentado = 0;
                        if (HelperFechas.EsFinDeSemanaOFeriado(diaCalendario))
                        {
                            //Días feriados y fin de semana no hay acreditaciones
                        }
                        else
                        {
                            OperacionesTarjetasCredito.CompletarTablasCalendario(listaTarjetas, diaCalendario, ref tablaPercepcionesYRetenciones, out totalAcreditado, out totalPresentado);
                        }
                        if (Global.ModoTraza)
                        {
                            Logger.Default.Info(String.Format("Información recuperada para el día: {0}", diaCalendario));
                            Logger.Default.Info(String.Format("     TotalAcreditado: {0}", totalAcreditado));
                            Logger.Default.Info(String.Format("     TotalPresentado: {0}", totalPresentado));
                        }
                        Models.AcreditacionCalendario resultado = new Models.AcreditacionCalendario();
                        resultado.Fecha = diaCalendario.ToString("dd/MM/yyyy");
                        resultado.TotalAcreditado = totalAcreditado;
                        resultado.TotalPresentado = totalPresentado;
                        listaAcreditaciones.Add(resultado);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(ExceptionManager.GetExceptionString(ex));
                }
                return (listaAcreditaciones.ToArray());
            }, null, name: "(uso interno)");
        }
    }
}