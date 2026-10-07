using System;
using System.Collections.Generic;
using System.Text;

namespace PriceServer
{
    public class ClientInfo
    {
        public string EndPoint { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastActivity { get; set; }
        public List<DateTime> RequestTimestamps { get; set; } = new List<DateTime>();

        public ClientInfo(string endPoint)
        {
            EndPoint = endPoint;
            FirstSeen = DateTime.Now;
            LastActivity = DateTime.Now;
        }
        public int RequestsInLastHour()
        {
            var oneHourAgo = DateTime.Now.AddHours(-1);
            return RequestTimestamps.FindAll(t => t > oneHourAgo).Count;
        }
        public void RecordRequest()
        {
            RequestTimestamps.Add(DateTime.Now);
            LastActivity = DateTime.Now;

            var oneHourAgo = DateTime.Now.AddHours(-1);
            RequestTimestamps.RemoveAll(t => t < oneHourAgo);
        }
        public int SecondsUntilNextAllowed()
        {
            var oneHourAgo = DateTime.Now.AddHours(-1);
            var recent = RequestTimestamps.FindAll(t => t > oneHourAgo);

            if (recent.Count < 10) return 0;

            var oldest = recent[0];
            var freeAt = oldest.AddHours(1);
            var wait = (freeAt - DateTime.Now).TotalSeconds;
            return wait > 0 ? (int)wait : 0;
        }
        public bool IsIdle(TimeSpan timeout)
        {
            return DateTime.Now - LastActivity > timeout;
        }
    }
}
