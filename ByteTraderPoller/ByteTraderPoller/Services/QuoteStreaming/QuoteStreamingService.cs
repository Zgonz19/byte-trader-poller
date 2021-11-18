using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteTraderPoller.Services.QuoteStreaming
{
    public class QuoteStreamingService
    {
        QuoteStreamingManager StreamingManager = new QuoteStreamingManager();
        public async Task Initialize()
        {
            ServiceInitializer();
        }


        public void ServiceInitializer()
        {
            while (true)
            {
                var currentTime = DateTime.Now;
                switch (currentTime.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                    case DayOfWeek.Tuesday:
                    case DayOfWeek.Wednesday:
                    case DayOfWeek.Thursday:
                    case DayOfWeek.Friday:
                        InitializeTrackers(currentTime);
                        break;
                    case DayOfWeek.Saturday:
                    case DayOfWeek.Sunday:
                        //InitializeTrackers(currentTime);//
                        break;
                }
                var now = DateTime.Now;
                var tomorrow = DateTime.Now.AddDays(1).Date;
                if (now < tomorrow)
                {
                    Thread.Sleep(tomorrow - now);
                }
                Thread.Sleep(new TimeSpan(7, 0, 0));
            }
        }
        public void InitializeTrackers(DateTime date)
        {
            var time = date.TimeOfDay;
            if (time < new TimeSpan(8, 30, 0))
            {
                Thread.Sleep(new TimeSpan(8, 30, 0) - time);
                Task.Run(() => StreamingManager.InitializeManager());
                Thread.Sleep(new TimeSpan(15, 0, 0) - DateTime.Now.TimeOfDay);
                StreamingManager.TdaWebsocket.LoginSuccess = false;
            }
            else if (time >= new TimeSpan(8, 30, 0) && time < new TimeSpan(15, 0, 0))
            {
                Task.Run(() => StreamingManager.InitializeManager());
                Thread.Sleep(new TimeSpan(15, 0, 0) - time);
                StreamingManager.TdaWebsocket.LoginSuccess = false;
            }
            else if (time >= new TimeSpan(15, 0, 0))
            {
                StreamingManager.TdaWebsocket.LoginSuccess = false;
            }
        }
    }
}
