using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleDepInj
{
    class Program
    {
        const string clientA = "ClientA";
        const string clientB = "ClientB";
        public static void Main(string[] args)
        {
            //setup our DI
            var serviceProvider = SetUpDepInjectionServices();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            for (int waitedCalls = 0; waitedCalls < 2; waitedCalls++)
            {
                for (int i = 0; i < 3; i++)
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        Console.WriteLine($"Scope Start:{scope.GetHashCode()}");

                        IServiceProvider scopeServiceProvider = scope.ServiceProvider;

                        for (int invocation = 0; invocation < 2; invocation++)
                        {
                            Console.WriteLine($"Invocation:{invocation + 1}");
                            var namedHttpClientA = GetNamedClient(scopeServiceProvider, clientA);
                            var namedHttpClientB = GetNamedClient(scopeServiceProvider, clientB);

                            Console.WriteLine($"NamedClientA:{namedHttpClientA.GetHashCode()}");
                            Console.WriteLine($"NamedClientB:{namedHttpClientB.GetHashCode()}");

                            namedHttpClientA.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost"));
                            namedHttpClientB.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost"));

                            var typedHttpClientA = GetTypedClient<TestHttpClientA>(scopeServiceProvider);
                            var typedHttpClientB = GetTypedClient<TestHttpClientB>(scopeServiceProvider);

                            Console.WriteLine($"TypedClientA:{typedHttpClientA.GetHashCode()}");
                            Console.WriteLine($"TypedClientB:{typedHttpClientB.GetHashCode()}");

                            var a = typedHttpClientA.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost"), CancellationToken.None).Result;
                            var b = typedHttpClientB.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost"), CancellationToken.None).Result;      

                        }

                        Console.WriteLine($"Scope End:{scope.GetHashCode()}");
                        Console.WriteLine();
                    }
                }

                Thread.Sleep(2 * 60 * 1000);
                Console.WriteLine("************** After 2 minutes ****************");
            }
        }

        private static HttpClient GetNamedClient(IServiceProvider serviceProvider, string clientName)
        {
            return serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);
        }

        private static T GetTypedClient<T>(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<T>();
        }

        private static ServiceProvider SetUpDepInjectionServices()
        {
            var serviceColl = new ServiceCollection();
            serviceColl.AddTransient<TestHttpMessageHandler>();
            serviceColl.AddHttpClient(clientA).ConfigurePrimaryHttpMessageHandler<TestHttpMessageHandler>();
            serviceColl.AddHttpClient(clientB).ConfigurePrimaryHttpMessageHandler<TestHttpMessageHandler>();
            serviceColl.AddHttpClient<TestHttpClientA>().ConfigurePrimaryHttpMessageHandler<TestHttpMessageHandler>();
            serviceColl.AddHttpClient<TestHttpClientB>().ConfigurePrimaryHttpMessageHandler<TestHttpMessageHandler>();
            return serviceColl.BuildServiceProvider();
        }
    }

    public class TestHttpMessageHandler: HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestMessageHandler:{this.GetHashCode()}");
            return Task.FromResult(new HttpResponseMessage());
        }
    }

    public class TestHttpClientA
    {
        private readonly HttpClient httpClient;
        public TestHttpClientA(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestHttpClientA:{this.httpClient.GetHashCode()}");
            return await this.httpClient.SendAsync(request, cancellationToken);
        }
    }

    public class TestHttpClientB
    {
        private readonly HttpClient httpClient;
        public TestHttpClientB(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"TestHttpClientB:{this.httpClient.GetHashCode()}");
            return await this.httpClient.SendAsync(request, cancellationToken);
        }
    }
}
