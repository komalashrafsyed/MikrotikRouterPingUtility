using System;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using tik4net;
using tik4net.Objects.Tool;
using tik4net.Objects;

  
namespace MikrotikAPIPing
{
    public class EventMessageModel
    {
        public EventMessageModel(ToolPing reply, string sourceipaddress, string destipaddress, int ipid)
        {
            ipID = ipid;
            sourceipAddress = sourceipaddress;
            destipAddress = destipaddress;
            

            if (reply.Received != null)
            {
                roundTripTime = reply.AvgRtt;
                timetoLive = reply.TimeToLife;
                MinRtt = reply.MinRtt;
                MaxRtt = reply.MaxRtt;
                Received = reply.Received;
                Sent = reply.Sent;
                Time = reply.Time;
                Host = reply.Host;
                SequenceNo = reply.SequenceNo;

            }

            //EventId = DateTime.Now.Day +  DateTime.Now.Minute;
            CorrelationId = Guid.NewGuid();
            EventTime = DateTime.Now;
        }

        public int ipID { get; set; }
        public string sourceipAddress { get; set; }
        public string destipAddress { get; set; }
        public string pingStatus { get; set; }
        public string roundTripTime { get; set; }
        public string timetoLive { get; set; }


        public string MaxRtt { get; set; }
        public string MinRtt { get; set; }
        public string Received { get; set; }
        public string Sent { get; set; }



        public string Time { get; set; }
        public string Host { get; set; }
        public long SequenceNo { get; set; }
       

        // public int EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime EventTime { get; set; }

        //public string DisplayFormat => $"EventId: {EventId} | EventTime: {EventTime:HH:mm:ss} | CorrelationId: {CorrelationId} | Message: {Message}";

    }
}
