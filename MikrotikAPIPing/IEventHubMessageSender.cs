using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MikrotikAPIPing
{
    public interface IEventHubMessageSender
    {
        Task SendAsync(EventMessageModel message);

        Task SendAsync(IEnumerable<EventMessageModel> messages);
    }
}
