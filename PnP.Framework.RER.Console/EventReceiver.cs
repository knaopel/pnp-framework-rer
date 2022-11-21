using Microsoft.SharePoint.Client;

namespace PnP.Framework.RER.Console
{
    internal class EventReceiver
    {
        public string ReceiverName { get; set; }
        public string ReceiverUrl { get; set; }
        public EventReceiverType EventReceiverType { get; set; }
    }
}
