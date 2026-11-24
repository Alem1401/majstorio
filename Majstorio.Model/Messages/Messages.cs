using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majstorio.Model.Messages
{
    public class NotificationMessage
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public int? ServiceRequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
