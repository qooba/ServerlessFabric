using System.Net;
using System.Net.Http.Headers;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorResponseMessage
    {
        HttpStatusCode StatusCode { get; set; }

        HttpResponseHeaders Headers { get; set; }

        string ErrorMessage { get; set; }
    }
}
