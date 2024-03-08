using Nancy;
using System.Collections.Generic;
using System;
using Nancy.Security;
using Aoniken.CaldenOil.Entidades;
using Aoniken.CaldenOil.ReglasNegocio;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class ArticulosModule : NancyModule
    {
        public ArticulosModule() : base("api/Articulos/")
        {

            Get<Models.Articulo[]>("GetAllCombustibles", p =>
            {
                this.RequiresAuthentication();
                List<Models.Articulo> articulosLista = HelperSQL.GetListaArticulosCombustibles();
                return (articulosLista.ToArray());
            }, null, name: "Retorna la lista de todos los artículos marcados como 'combustibles' ");

            Get<Models.ArticuloCompleto[]>("GetAllLubricantes", p =>
            {
                this.RequiresAuthentication();
                List<Models.ArticuloCompleto> articulosLista = HelperSQL.GetListaArticulosLubricantes();
                return (articulosLista.ToArray());
            }, null, name: "Retorna la lista de todos los artículos marcados como 'Lubricantes' ");

            Get<Models.ArticuloCompleto[]>("GetArticulos", p =>
            {
                this.RequiresAuthentication();
                List<Models.ArticuloCompleto> articulosLista = null;
                try
                {
                    int? idGrupoArticulo = this.Request.Query["idGrupoArticulo"];
                    int? idFamiliaArticulo = this.Request.Query["idFamiliaArticulo"];
                    articulosLista = HelperSQL.GetArticulos(idGrupoArticulo, idFamiliaArticulo);
                }
                catch
                { }
                return (articulosLista.ToArray());
            }, null, name: "Retorna la lista de artículos. Parámetros opcionales: {idGrupoArticulo} {idFamiliaArticulo}");

            Get<Models.GruposArticulos[]>("GetGruposArticulos", p =>
            {
                this.RequiresAuthentication();
                List<Models.GruposArticulos> gruposLista = HelperSQL.GetListaGruposArticulos();
                return (gruposLista.ToArray());
            }, null, name: "Retorna la lista de todos los Grupos de artículos ");

            Get<Models.FamiliaArticulo[]>("GetFamiliasArticulos", p =>
            {
                this.RequiresAuthentication();
                List<Models.FamiliaArticulo> familiasLista = HelperSQL.GetListaFamiliasArticulos();
                return (familiasLista.ToArray());
            }, null, name: "Retorna la lista de todas las Familias de artículos ");

            Get<Models.Articulo>("GetArticulo", p =>
            {
                this.RequiresAuthentication();
                Models.Articulo result = null;
                try
                {
                    int? idArticulo = this.Request.Query["idArticulo"];
                    if (idArticulo != null)
                    {
                        result = HelperSQL.GetArticuloPorId(idArticulo.Value);
                    }
                }
                catch
                { }
                return (result);
            }, null, name: "Retorna el detalle de un artículo. Parámetros: {idArticulo}");

            Post<Models.Respuesta>("PersistiGrupoAfinidadCliente", p =>
            {
                this.RequiresAuthentication();

                int idGrupoAfinidadCliente = -1;

                try
                {
                    string descripcion = this.Request.Query["Descripcion"];
                    idGrupoAfinidadCliente = HelperSQL.InsertarGrupoAfinidadCliente(descripcion);

                    return new Models.Respuesta
                    {
                        Valor = true,
                        Texto = $"Se insertó con éxito el GrupoAfinidadCliente, IdGrupoAfinidadCliente {idGrupoAfinidadCliente}",
                        Comprobante = idGrupoAfinidadCliente.ToString(),
                        Total = 1
                    };
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
            }, null, name: "Inserta un Grupo de Afinidad de clientes");

            Put<Models.Respuesta>("PersistiGrupoAfinidadDescuento", p =>
            {
                this.RequiresAuthentication();

                int idDescuento = -1;

                try
                {
                    int idGrupoAfinidad = this.Request.Query["IdGrupoAfinidad"];
                    int idGrupoArticulo = this.Request.Query["IdGrupoArticulo"];
                    decimal porcentajeDescuento = this.Request.Query["PorcentajeDescuento"];
                    string observacion = this.Request.Query["Observacion"];
                    idDescuento = HelperSQL.InsertarGrupoAfinidadDescuento(idGrupoAfinidad, idGrupoArticulo, porcentajeDescuento, observacion);

                    return new Models.Respuesta
                    {
                        Valor = true,
                        Texto = $"Se insertó o actualizó con éxito el GrupoAfinidadDescuento, IdDescuento {idDescuento}",
                        Comprobante = idDescuento.ToString(),
                        Total = 1
                    };
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
            }, null, name: "Inserta o actualiza un Descuento asociado a un Grupo de Afinidad de clientes");

            Put<Models.Respuesta>("AsignarGrupoAfinidadCliente", p =>
            {
                this.RequiresAuthentication();

                int cantidad = 0;

                try
                {
                    int idCliente = this.Request.Query["IdCliente"];
                    int idGrupoAfinidadCliente = this.Request.Query["IdGrupoAfinidadCliente"];
                    
                    cantidad = HelperSQL.AsignarGrupoAfinidadCliente(idCliente, idGrupoAfinidadCliente);

                    return new Models.Respuesta
                    {
                        Valor = true,
                        Texto = $"Se asignó con éxito el GrupoAfinidadCliente al Cliente",
                        Total = 1
                    };
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
            }, null, name: "Asigna un Grupo de Afinidad a un Cliente");

            Get<Response>("", p =>
            {
                return new Response() { StatusCode = HttpStatusCode.BadRequest };
            });
        }
    }
}