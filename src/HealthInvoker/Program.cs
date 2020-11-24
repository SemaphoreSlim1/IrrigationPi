using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HealthInvoker
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            using (var cli = new HttpClient())
            {
                cli.BaseAddress = new Uri("http://localhost/");

                try
                {
                    var response = await cli.GetAsync("");
                    return response.IsSuccessStatusCode ? 0 : 1;
                }
                catch
                {
                    return 1;
                }
            }
        }
    }
}
