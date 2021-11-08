using System;

namespace ByteTraderPoller
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Byte Trader Poller...");
            var serviceManger = new ServiceManager();
            serviceManger.StartServices();
        }
    }
}
