using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLingo.Domain.Exceptions
{
    public sealed class UnauthorizedActionException : AppException
    {
        public UnauthorizedActionException(string message) : base(message) { }
    }
}
