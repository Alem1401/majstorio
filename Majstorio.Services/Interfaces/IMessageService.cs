using Majstorio.Model.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majstorio.Services.Interfaces
{
    public interface IMessageService
    {
        Task PublishAsync(NotificationMessage message);
    }
}
