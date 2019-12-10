using System;
using System.Collections.Generic;
using System.Text;

namespace MikrotikAPIPing
{
    public class EventHubConfiguration
    {
        public EventHubConfiguration(string connectionString, string hubName)
        {
            ConnectionString = connectionString;
            HubName = hubName;
        }

        public string ConnectionString { get; }
        public string HubName { get; }

    }
}
