using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace HealthInvoker
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            using (var cli = new HttpClient())
            {
                cli.BaseAddress = new Uri("http://localhost/");

                try
                {
                    var response = await cli.GetAsync("Health");
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
