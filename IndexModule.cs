using Nancy;
using Vemn.Framework.Logging;

namespace HostCaldenONNancy.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule() 
        {
            Get("/", parameters =>
            {
                return View["index"];
            });

            Get("api/Dominios/IsServerAlive", p =>
            {
                return true;
            }, null, name: "Devuelve True si el servidor API está online.");
        }

        public static void LoguearRequest(Request request)
        {            
            if (request == null)
            {
                Logger.Default.Debug("No se han encontrados respuesta en request");
            }
            else
            {
                Nancy.IO.RequestStream requestStream = request.Body as Nancy.IO.RequestStream;
                if (requestStream != null)
                {
                    Logger.Default.DebugFormat("request.Body: {0}", requestStream.ReadAsString());
                }
                LoguearRequestQuery(request.Form);
                RequestHeaders headers = request.Headers;
                foreach (string key in headers.Keys)
                {
                    Logger.Default.DebugFormat("this.request.Headers['{1}']: {0}", headers[key], key);
                }
                Logger.Default.DebugFormat("request.Method: {0}", request.Method);
                Logger.Default.DebugFormat("request.Path: {0}", request.Path);                
                Logger.Default.DebugFormat("request.Url: {0}", request.Url);
            }
        }

        public static void LoguearRequestQuery(dynamic requestQuery)
        {
            Nancy.DynamicDictionary query = requestQuery as Nancy.DynamicDictionary;
            if (query == null)
            {
                Logger.Default.Debug("No se han encontrados claves en this.Request.Query");
            }
            else
            {
                Logger.Default.DebugFormat("this.Request.Query.Count: {0}", query.Keys.Count);
                foreach (string key in query.Keys)
                {
                    Logger.Default.DebugFormat("this.Request.Query['{1}']: {0}", query[key], key);
                }
            }
        }
    }    
}