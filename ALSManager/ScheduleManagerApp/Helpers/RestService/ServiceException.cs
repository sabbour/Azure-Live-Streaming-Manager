using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerManagerApp.Helpers.RESTService
{
    public class ServiceException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public ServiceException(string message)
            : base(message)
        {
        }
        public ServiceException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
