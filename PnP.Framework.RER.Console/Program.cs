using Microsoft.Extensions.Configuration;
using Microsoft.SharePoint.Client;
using System.Linq;
using System.Threading.Tasks;

namespace PnP.Framework.RER.Console
{
    class Program
    {
        private const string RECEIVER_URL = "https://300a-98-209-39-163.ngrok.io/api/ProcessItemEvents";

        public static EventReceiver[] _events = new EventReceiver[] {
            new EventReceiver { ReceiverName = "TestRERItemAdded", ReceiverUrl = RECEIVER_URL, EventReceiverType = EventReceiverType.ItemAdded },
            new EventReceiver { ReceiverName = "TestRERItemUpdated", ReceiverUrl = RECEIVER_URL, EventReceiverType = EventReceiverType.ItemUpdated }
        };

        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.local.json", true)
                .AddUserSecrets<Program>()
                .Build();
            var creds = configuration.Get<SharePointAppCreds>();

            var authManager = new AuthenticationManager();

            var context = authManager.GetACSAppOnlyContext(creds.SiteUrl, creds.ClientId, creds.ClientSecret);
            context.Load(context.Web);
            var list = context.Web.GetListByUrl("Lists/TestList");
            await context.ExecuteQueryRetryAsync();

            // To add the RERs
            foreach (EventReceiver evnt in _events)
            {
                await AddEventReceiver(list, evnt.ReceiverName, evnt.ReceiverUrl, evnt.EventReceiverType);
            }

            // To Remove the RERs, uncomment the following three lines
            //foreach (EventReceiver evnt in _events)
            //{
            //    await RemoveEventReceiver(list, evnt.ReceiverName);
            //}
        }

        private static async Task AddEventReceiver(List list, string name, string url, EventReceiverType type)
        {
            var eventReceiver =
                new EventReceiverDefinitionCreationInformation
                {
                    EventType = type,
                    ReceiverName = name,
                    ReceiverUrl = url,
                    SequenceNumber = 1000
                };

            list.EventReceivers.Add(eventReceiver);

            await list.Context.ExecuteQueryRetryAsync();
        }

        private static async Task RemoveEventReceiver(List list, string name)
        {
            var receivers = list.EventReceivers;
            list.Context.Load(receivers);
            await list.Context.ExecuteQueryRetryAsync();

            var toDelete = receivers.ToList().FirstOrDefault(r => r.ReceiverName == name);
            if (toDelete != null)
            {
                toDelete.DeleteObject();
                await list.Context.ExecuteQueryRetryAsync();
            }
        }
    }
}
