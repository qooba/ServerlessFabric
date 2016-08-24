# ServerlessFabric
.NET library for creating serverless actors microservices using Azure Functions

Client:

```csharp
var actor = Qooba.ServerlessFabric.ActorFactory.Create<ISmsSender>(new Uri("https://{myFunction}.azurewebsites.net/api/{myFunctionName}"));
```

Function:

```csharp
using System.Net;
using Qooba.ServerlessFabric;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log) => await ActorService<IMyActorInterface>.Create(req, () => new MyActorImplementation());
```
