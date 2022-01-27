//using Microsoft.Azure.ServiceBus;
using Azure.Messaging.ServiceBus;
using System.Text;

namespace joseevillasmil.IOT.AndroidTest.IOT
{
    public class IotQueueFunctions
    {
        private ServiceBusClient sbClient;
        private ServiceBusSender sender;
        //private QueueClient sender;
        public IotQueueFunctions()
        {
        }

        public IotQueueFunctions(string _queueConStr, string _queueName)
        {
            sbClient = new ServiceBusClient(_queueConStr);
            sender = sbClient.CreateSender(_queueName);

        }

        public void SendMessage(string message)
        {
            try
            {
                byte[] _msg = Encoding.UTF8.GetBytes(message);
                ServiceBusMessage msg = new ServiceBusMessage(_msg);
                sender.SendMessageAsync(msg).GetAwaiter().GetResult();
                
            }
            catch { }
            

        }
    }
}