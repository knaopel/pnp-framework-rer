using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace PnP.Framework.RER.Functions
{
    public class RemoteEventFunctions
    {
        private readonly ILogger _logger;

        public RemoteEventFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoteEventFunctions>();
        }

        [Function("ProcessItemEvents")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
