using Jose;
using Nancy;
using Nancy.Security;
using System;
using System.Collections.Generic;
using Aoniken.CaldenOil.ReglasNegocio;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class VendedoresModule : NancyModule
    {
        public VendedoresModule() : base("api/Vendedores/")
        {
            Get<Models.GruposArticulos[]>("getGruposArticulosHabilitados", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "getGruposArticulosHabilitados", null);
                this.RequiresAuthentication();
                return HelperSQL.GruposHabilitados().ToArray();
            }, null, name: "Recuperar la lista de grupos de artículos habilitados para SalesMan (Vendedores).");

            Get<Models.Vendedor>("IsVendedorValido", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "IsVendedorValido", null);
                Models.Usuario usuario = null;
                Models.AuthToken authToken = null;
                string token = string.Empty;
                string nombre = this.Request.Query["nombre"];
                string password = this.Request.Query["password"];
                if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(password))
                {
                    usuario = HelperSQL.ValidarVendedor(nombre, password);
                }
                if (usuario != null)
                {
                    authToken = new Models.AuthToken()
                    {
                        UserId = usuario.SyncGUID,
                        UserLogin = usuario.Nombre,
                        UserName = usuario.Nombre,
                        ExpirationDateTime = DateTime.Now.AddDays(365)
                    };
                    object key = new Models.AuthSettings().SecretKey;
                    token = Jose.JWT.Encode(authToken, key, JwsAlgorithm.HS256);

                    Models.Vendedor response = new Models.Vendedor()
                    {
                        IdEmpleado = usuario.IdUsuario,
                        Nombre = usuario.Nombre,
                        Token = token,
                        ListaPrecioPorCliente = usuario.ListaPrecioPorCliente,
                        Salesman_PermiteModificarPrecio = usuario.Salesman_PermiteModificarPrecio,
                        IdGrupoVendedores = usuario.IdGrupoVendedores
                    };

                    HelperSQL.RecuperarDatosGenerales(out string razonSocial, out byte[] logoEmpresa);

                    response.RazonSocialEstacion = razonSocial;
                    response.LogoEmpresa = logoEmpresa;

                    return response;
                }
                else
                {
                    return null;
                }
                //turn (token);
            }, null, name: "Valida un vendedor dado su usuario y contraseña. Si es válido, la respuesta incluye el Token para autenticar el resto de las operaciones.  Parámetros: {nombre} {password}");

            Get<Models.ArticuloInfo[]>("GetArticulosHabilitados", p =>
            {
                // this.RequiresAuthentication();
                TextHelper.LoggearMetodo(p, this.Request, "GetArticulosHabilitados", null);
                string descripcion = this.Request.Query["descripcion"];
                bool controlaStock = this.Request.Query["controlaStock"];

                List<Models.ArticuloInfo> articulosLista = HelperSQL.GetListaArticulosHabilitados(descripcion);

                if (controlaStock)
                {
                    foreach (Models.ArticuloInfo articulo in articulosLista)
                    {
                        try
                        {
                            articulo.Stock = OperacionesStock.ConsultarSaldoStock(articulo.IdArticulo, null, null);
                        }
                        catch (Exception)
                        {
                            articulo.Stock = 0;
                        }

                    }
                }
                return (articulosLista.ToArray());
            }, null, name: "Recupera la lista de articulos ACTIVOS y habilitados para su uso en la App de Vendedores.  Parámetros: {descripcion} {controlaStock:booleano}");

            Get<Models.ArticuloInfo[]>("GetArticulosHabilitadosPorDeposito", p =>
            {
                // this.RequiresAuthentication();
                TextHelper.LoggearMetodo(p, this.Request, "GetArticulosHabilitadosPorDeposito", null);
                string descripcion = this.Request.Query["descripcion"];
                bool controlaStock = this.Request.Query["controlaStock"];
                int idDeposito = this.Request.Query["idDeposito"];

                List<Models.ArticuloInfo> articulosLista = HelperSQL.GetListaArticulosHabilitados(descripcion);

                if (controlaStock)
                {
                    foreach (Models.ArticuloInfo articulo in articulosLista)
                    {
                        try
                        {
                            articulo.Stock = OperacionesStock.ConsultarSaldoStock(articulo.IdArticulo, null, idDeposito);
                        }
                        catch (Exception)
                        {
                            articulo.Stock = 0;
                        }

                    }
                }
                return (articulosLista.ToArray());
            }, null, name: "Recupera la lista de articulos ACTIVOS y habilitados para su uso en la App de Vendedores, incluyendo su saldo de stock dado el IdDeposito pasado como parámetro.  Parámetros: {descripcion} {controlaStock:booleano} {idDeposito}");

            Get<Models.Cliente[]>("GetClientes", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetClientes", null);
                this.RequiresAuthentication();
                int idVendedor = this.Request.Query["idVendedor"];
                
                List<Models.Cliente> clientesLista = HelperSQL.GetListaClientes(idVendedor);
                
                return (clientesLista.ToArray());
            }, null, name: "Recupera la lista de clientes asociados al IdVendedor pasado como parámetro.  Parámetros: {idVendedor}");

            Get<Models.ArticuloStock[]>("GetArticulosStock", p =>
            {
                // this.RequiresAuthentication();
                TextHelper.LoggearMetodo(p, this.Request, "GetArticulosStock", null);
                int idArticulo = this.Request.Query["idArticulo"];
                int idEmpleado = this.Request.Query["idEmpleado"];
                List<Models.ArticuloStock> listaArticuloStock = new List<Models.ArticuloStock>();
                List<Models.Deposito> depositosLista = HelperSQL.GetListaDepositosEmpleado(idEmpleado);
                foreach (var deposito in depositosLista)
                {
                    var articuloStock = new Models.ArticuloStock
                    {
                        IdArticulo = idArticulo,
                        IdEmpleado = idEmpleado,
                        IdDeposito = deposito.IdDeposito,
                        NombreDeposito = deposito.Descripcion
                    };
                    try
                    {
                        var stock = OperacionesStock.ConsultarSaldoStock(idArticulo, null, deposito.IdDeposito);
                        articuloStock.Stock = stock;
                    }
                    catch (Exception)
                    {
                        articuloStock.Stock = 0;
                    }
                    listaArticuloStock.Add(articuloStock);
                }
                return listaArticuloStock.ToArray();
            });

            Get<Models.Cliente[]>("GetClientesPorRazonSocial", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetClientesPorRazonSocial", null);
                this.RequiresAuthentication();
                string razonsocial = this.Request.Query["razonsocial"];
                int idVendedor = this.Request.Query["idVendedor"];

                List<Models.Cliente> clientesLista = HelperSQL.GetListaClientes(razonsocial, idVendedor);
                return (clientesLista.ToArray());
            }, null, name: "Recupera la lista de clientes asociados al IdVendedor pasado como parámetro, filtrados por razón social.  Parámetros: {razonsocial} {idVendedor}");

            Get<Models.ClienteDomicilioEntrega[]>("GetDomiciliosDeEntrega", 
                p => {
                    TextHelper.LoggearMetodo(p, this.Request, "GetDomiciliosDeEntrega", null);
                    this.RequiresAuthentication();
                    int idCliente = this.Request.Query["idCliente"];

                    //  idDomicilioEntrega

                    List<Models.ClienteDomicilioEntrega> domiciliosEntregaLista = HelperSQL.GetDomiciliosDeEntrega(idCliente);
                    return (domiciliosEntregaLista.ToArray());
                }, null, name: "Devuelve la lista de Domicilios de entrega de UN cliente");


            Get<decimal[]>("GetDescuentosVendedor", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "GetDescuentosVendedor", null);
                int idEmpleado = this.Request.Query["idEmpleado"];
                List<decimal> descuentosLista = HelperSQL.GetListaDescuentosEmpleado(idEmpleado);

                return descuentosLista.ToArray();
            });

            Get<Models.ComisionPorObjetivo[]>("GetComisionesPorObjetivos", p=>
            {
                try
                {
                    TextHelper.LoggearMetodo(p, this.Request, "GetComisionesPorObjetivos", null);
                    this.RequiresAuthentication();

                    List<Models.ComisionPorObjetivo> comisionesPorObjetivosLista = HelperSQL.GetListaComisionesPorObjetivos();
                    return (comisionesPorObjetivosLista.ToArray());
                }
                catch (Exception ex)
                {
                    Logger.Default.Error(Vemn.Framework.ExceptionManagement.ExceptionManager.GetExceptionStringNoAssemblies(ex));
                    throw;
                }
            }, null, name: "Recupera la lista de Comisiones por Objetivos (TipoObjetivo: 0: En pesos - 1: En unidades - 2: En unidades por bulto)");

            Get<Models.ComisionPorObjetivoValorVentaEmpleado[]>("ComisionesPorObjetivosValorVentaEmpleado", p =>
            {
                TextHelper.LoggearMetodo(p, this.Request, "ComisionesPorObjetivosValorVentaEmpleado", null);
                this.RequiresAuthentication();

                int idEmpleado = this.Request.Query["idEmpleado"];
                int idComisionPorObjetivo = this.Request.Query["idComisionPorObjetivo"];
                DateTime? fechaInicial = this.Request.Query["fechaInicial"];
                DateTime? fechaFinal = this.Request.Query["fechaFinal"];

                List<Models.ComisionPorObjetivoValorVentaEmpleado> comisionesPorObjetivosValorVentaEmpleadoLista =
                HelperSQL.GetListaComisionesPorObjetivosValorVentaEmpleado(idEmpleado, idComisionPorObjetivo, fechaInicial ?? System.Data.SqlTypes.SqlDateTime.MinValue.Value, fechaFinal ?? DateTime.Now);

                return (comisionesPorObjetivosValorVentaEmpleadoLista.ToArray());
            },null, name: "Recupera la lista de Comisiones por Objetivos incluyendo la venta por empleado (TipoObjetivo: 0: En pesos - 1: En unidades - 2: En unidades por bulto)");

            //Get<Models.ComisionPorObjetivoAlicuota[]>("ComisionesPorObjetivosAlicuotas", p =>
            //{
            //    this.RequiresAuthentication();

            //    List<Models.ComisionPorObjetivoAlicuota> comisionesPorObjetivosAlicuotasLista = HelperSQL.GetListaComisionesPorObjetivosAlicuotas();

            //    //  hidrato a mano el campo MaxObjetivo
            //    for (int i = 0; i < comisionesPorObjetivosAlicuotasLista.Count; i++)
            //    {
            //        Models.ComisionPorObjetivoAlicuota c = comisionesPorObjetivosAlicuotasLista[i];

            //        List<decimal> lista = new List<decimal>() { c.Objetivo1??0, c.Objetivo2 ?? 0, c.Objetivo3 ?? 0, c.Objetivo4 ?? 0, c.Objetivo5 ?? 0, c.Objetivo6 ?? 0, c.Objetivo7 ?? 0, c.Objetivo8 ?? 0, c.Objetivo9 ?? 0, c.Objetivo10 ?? 0 };
            //        decimal maxValue = lista.Max();
            //        comisionesPorObjetivosAlicuotasLista[i].MaxObjetivo = maxValue;
            //    }

            //    return (comisionesPorObjetivosAlicuotasLista.ToArray());
            //}, null, name: "Recupera la lista de Alícuotas de Comisiones por Objetivos");

        }
    }
}