using Nancy;
using System.Net.Http;

namespace HostCaldenONNancy.Modules
{
    public class RedencionModule : NancyModule
    {
        public RedencionModule() : base("/api/Redencion/")
        {
            Post("", async p =>
            {
                using (var client = new RestClient.RestClientDotNet())
                {
                    string endpoint = $"https://test_raizen-rest.thalamuslive.com/{client}/api/v4/mileage/shell_box/cart/addItemsToCartAndCheckout?touchpoint=shell_box_puntos&token=r8avwbs59s330xuvqxzmgr79p651p5854vjyuzw7vvxb6b74vy65i47g0aluki1k";
                    using (Nancy.IO.RequestStream requestStream = this.Request.Body as Nancy.IO.RequestStream)
                    {
                        string body = requestStream.ReadAsString();
                        HttpResponseMessage response = client.Post(endpoint, body, null);
                        if (response.IsSuccessStatusCode)
                        {
                            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        }
                    }
                }
            });
        }
    }
}
