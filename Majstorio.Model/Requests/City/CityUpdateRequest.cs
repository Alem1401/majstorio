using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majstorio.Model.Requests.City
{
    public class CityUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool isActive { get; set; }
    }
}
