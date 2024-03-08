using Nancy;
using Nancy.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Prueba.Nancy.Swagger
{
    public class DocMudule : NancyModule
    {
        private IRouteCacheProvider _routeCacheProvider;

        public DocMudule(IRouteCacheProvider routeCacheProvider) : base("/docs")
        {
            this._routeCacheProvider = routeCacheProvider;

            Get("/", _ =>
            {
                //var routeDescriptionList = _routeCacheProvider
                //                             .GetCache()
                //                             .SelectMany(x => x.Value)
                //                             .Select(x => x.Item2)
                //                             .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                //                             .ToList();

                List<Documentacion> documentacion = new List<Documentacion>();

                foreach (var item in _routeCacheProvider.GetCache())
                {
                    foreach (var valor in item.Value)
                    {
                        //if (!string.IsNullOrWhiteSpace(valor.Item2.Name))
                        {
                            documentacion.Add(new Documentacion() { Metodo = valor.Item2.Method, Path = valor.Item2.Path, Descripcion = valor.Item2.Name });
                        }
                    }
                }

                //return Response.AsJson(routeDescriptionList);
                return Response.AsJson(documentacion);
            },null, "Documentación en línea de la API");
        }

        class Documentacion
        {
            public string Metodo { get; set; }
            public string Path { get; set; }
            public string Descripcion { get; set; }
        }

    }
}