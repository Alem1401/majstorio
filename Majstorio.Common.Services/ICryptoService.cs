using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majstorio.Common.Services
{
    public interface ICryptoService
    {
        public string GenerateSlat();

        public string GenerateHash(string salt, string password);

        public bool VerifyPassword(string salt,string hash, string password);
    }
}
