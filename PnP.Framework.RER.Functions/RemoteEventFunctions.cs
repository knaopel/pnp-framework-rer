using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using PnP.Framework.RER.Common.EventReceivers;
using PnP.Framework.RER.Common.Helpers;
using PnP.Framework.RER.Common.Tokens;
using System.Net;
using System.Xml.Linq;

namespace PnP.Framework.RER.Functions
{
    public class RemoteEventFunctions
    {
        private readonly ILogger _logger;
        private readonly TokenManagerFactory _tokenManagerFactory;
        private readonly IHostingEnvironment _hostingEnvironment;


        public RemoteEventFunctions(ILoggerFactory loggerFactory, TokenManagerFactory tokenMangerFactory, IHostingEnvironment hostingEnvironment)
        {
            _logger = loggerFactory.CreateLogger<RemoteEventFunctions>();
            _tokenManagerFactory = tokenMangerFactory;
            _hostingEnvironment = hostingEnvironment;
        }

        [Function("ProcessItemEvents")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("C# HTTP trigger function processed a request.");
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var xdoc = XDocument.Parse(requestBody);

                var eventRoot = xdoc.Root.Descendants().First().Descendants().First();

                if (eventRoot.Name.LocalName != "ProcessEvent" && eventRoot.Name.LocalName != "ProcessOneWayEvent")
                {
                    throw new Exception($"Unable to resolve event type");
                }

                var payload = eventRoot.FirstNode.ToString();
                var eventProperties = SerializerHelper.Deserialize<SPRemoteEventProperties>(payload);

                var host = req.Url.Host;
                if (_hostingEnvironment.IsDevelopment())
                {
                    host = Environment.GetEnvironmentVariable("ngrokHost");
                }

                var tokenManager = _tokenManagerFactory.Create(eventProperties, host);

                var context = await tokenManager.GetUserClientContextAsync(eventProperties.ItemEventProperties.WebUrl);
                context.Load(context.Web);
                await context.ExecuteQueryRetryAsync();

                if (eventRoot.Name.LocalName == "ProcessEvent")
                {
                    return await ProcessSyncEvent(eventProperties, context, req);
                }

                if (eventRoot.Name.LocalName == "ProcessOneWayEvent")
                {
                    return await ProcessAsyncEvent(eventProperties, context, req);
                }

                throw new Exception($"Unable to resolve event type");
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(), ex, ex.Message);
                var result = new SPRemoteEventResult
                {
                    Status = SPRemoteEventServiceStatus.CancelWithError,
                    ErrorMessage = ex.Message
                };

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "text/xml;");
                response.WriteString(CreateEventResponse(result));

                return response;
            }
        }


        // -ing events, i.e ItemAdding
        private async Task<HttpResponseData> ProcessSyncEvent(SPRemoteEventProperties properties, ClientContext context, HttpRequestData req)
        {
            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        // do things
                        break;
                    }
                //etc
                default: { break; }
            }
            var result = new SPRemoteEventResult
            {
                Status = SPRemoteEventServiceStatus.Continue
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/xml;");
            response.WriteString(CreateEventResponse(result));

            return response;
        }

        // -ed events, i.e. ItemAdded
        private async Task<HttpResponseData> ProcessAsyncEvent(SPRemoteEventProperties properties, ClientContext context, HttpRequestData req)
        {
            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdded:
                    {
                        // do things
                        break;
                    }
                //etc
                default: { break; }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        private string CreateEventResponse(SPRemoteEventResult eventResult)
        {
            var responseTemplate = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
                                        <s:Body>{0}</s:Body>
                                    </s:Envelope>";
            var result = new ProcessEventResponse
            {
                ProcessEventResult = eventResult
            };
            var content = SerializerHelper.Serialize(result);

            return string.Format(responseTemplate, content);
        }
    }
}
