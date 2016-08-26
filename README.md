# ServerlessFabric
.NET library for creating serverless actors microservices using Azure Functions. 
In this approach each microservice (published as Azure Function) is a class with defined interface.
The library automatically creates the proxied and handlers. In fact you can create the application as a monolith (with correctly defined interfaces) 
and than split into microservices without much effort.

## Client side:

Let's assume that we have MVC application which has to send sms.
We have interface:

```csharp
public interface ISmsSender
{
    Task<string> SendSmsMulti(string request, int value, double value2);

    Task<string> SendSmsStr(string request);

    Task<SmsSendResponse> SendSms(SmsSendRequest request);

    Task<SmsMultipleSendResponse> SendMultipleSms(SmsMultipleSendRequest request);

    Task SendSmsNoResponse(SmsSendNoResponseRequest request);

    Task SendSmsNoRequestNoResponse();

    Task SendSmsMultiV(string request, int value, double value2);
}
```
 
 and requests and responses:

 ```csharp
public class SmsMultipleSendRequest
{
    public IList<string> PhoneNumbers { get; set; }

    public string Message { get; set; }
}

public class SmsMultipleSendResponse
{
    public bool Ok { get; set; }
}

public class SmsSendNoResponseRequest
{
    public string PhoneNumber { get; set; }

    public string Message { get; set; }
}

public class SmsSendRequest
{
    public string PhoneNumber { get; set; }

    public string Message { get; set; }
}

public class SmsSendResponse
{
    public bool Ok { get; set; }
}
```

And we have interface implementation:
```csharp
public class SmsSender : ISmsSender
{
    public async Task<SmsMultipleSendResponse> SendMultipleSms(SmsMultipleSendRequest request)
    {
        return await Task.FromResult(new SmsMultipleSendResponse { Ok = true });
    }

    public async Task<SmsSendResponse> SendSms(SmsSendRequest request)
    {
        return await Task.FromResult(new SmsSendResponse { Ok = true });
    }

    public async Task SendSmsNoRequestNoResponse()
    {
    }

    public async Task SendSmsNoResponse(SmsSendNoResponseRequest request)
    {
    }

    public async Task<string> SendSmsStr(string request)
    {
        return request;
    }

    public async Task<string> SendSmsMulti(string request, int value, double value2)
    {
        return request;
    }

    public async Task SendSmsMultiV(string request, int value, double value2)
    {
    }
}
```

!!! Please notice that ServerlessFabric supports only async methods !!!

Typically we register the interface implementation in IoC container in bootstrapp class:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add framework services.
    services.AddMvc();
    services.AddTransient<ISmsSender, SmsSender>();
}
```

and inject interface into constructor:

```csharp
public class HomeController : Controller
{
    private ISmsSender smsSender;
    public HomeController(ISmsSender smsSender)
    {
        this.smsSender = smsSender;
    }


    public async Task<IActionResult> Index()
    {
        var responseStr = await this.smsSender.SendSmsStr("Test");
        var responseMulti = await this.smsSender.SendSmsMulti("Test", 1, 1.0);
        await this.smsSender.SendSmsMultiV("Test", 1, 1.0);

        var response = await this.smsSender.SendSms(new SmsSendRequest { PhoneNumber = "555555555", Message = "My sms message" });
        var responseMultiple = await this.smsSender.SendMultipleSms(new SmsMultipleSendRequest { PhoneNumbers = new List<string>() { "555555555" }, Message = "My sms message" });
        await this.smsSender.SendSmsNoResponse(new SmsSendNoResponseRequest { PhoneNumber = "555555555", Message = "My sms message" });
        await this.smsSender.SendSmsNoRequestNoResponse();
        ViewBag.SmsOk = response.Ok;
        ViewBag.SmsMultipleOk = responseMultiple.Ok;
        return View();
    }
}
```

Ok our functionality ready but now we want to turn it into microservice.

We simply add nuget package:
[Qooba.ServerlessFabric](https://www.nuget.org/packages/Qooba.ServerlessFabric/)

```
Install-Package Qooba.ServerlessFabric -Pre
```

and change interface registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add framework services.
    services.AddMvc();
    //services.AddTransient<ISmsSender, SmsSender>();
    services.AddTransient<ISmsSender>(serviceProvider => Qooba.ServerlessFabric.ActorFactory.Create<ISmsSender>(new Uri("https://{functionServiceName}.azurewebsites.net/api/{functionName}?code={code is optional}")));
}
```

## Function side :

We have client ready so let's create function in Azure Portal.
We create new function service and call it: {functionServiceName}
Then we create C# Http triggered function and call it {functionName}.
We go to:
https://{functionServiceName}.scm.azurewebsites.net/dev

and in function folder create file project.json:

```csharp
{
    "frameworks": {
        "net46":{
            "dependencies": {
                "Qooba.ServerlessFabric": "1.0.0-alpha3"
            }
        }
    }
}
```

we also edit file run.csx and put:

```csharp
using System.Net;
using Qooba.ServerlessFabric;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var request = await req.Content.ReadAsStringAsync();
    log.Info(request);
    log.Info(req.RequestUri.ToString());

    try
    {
        return await ActorService<ISmsSender>.Create(req, () => new SmsSender(log));
    }
    catch (Exception ex)
    {
        log.Info(ex.Message);
        log.Info(ex.StackTrace);
        throw;
    }

}

public class SmsSender : ISmsSender
{
    private readonly TraceWriter log;
    public SmsSender(TraceWriter log)
    {
        this.log = log;
    }

    public async Task<SmsMultipleSendResponse> SendMultipleSms(SmsMultipleSendRequest request)
    {
        log.Info("Hello from SendMultipleSms !!!");
        return await Task.FromResult(new SmsMultipleSendResponse { Ok = true });
    }

    public async Task<SmsSendResponse> SendSms(SmsSendRequest request)
    {
        log.Info("Hello from SendSms !!!");
        return await Task.FromResult(new SmsSendResponse { Ok = true });
    }

    public async Task SendSmsNoRequestNoResponse()
    {
        log.Info("Hello from SendSmsNoRequestNoResponse !!!");
    }

    public async Task SendSmsNoResponse(SmsSendNoResponseRequest request)
    {
        log.Info("Hello from SendSmsNoResponse !!!");
    }

    public async Task<string> SendSmsStr(string request)
    {
        log.Info("Hello from SendSmsStr !!!");
        return request;
    }

    public async Task<string> SendSmsMulti(string request, int value, double value2)
    {
        log.Info("Hello from SendSmsMulti !!!");
        return request;
    }

    public async Task SendSmsMultiV(string request, int value, double value2)
    {
        log.Info("Hello from SendSmsMultiV !!!");
    }
}

public interface ISmsSender
{
    Task<string> SendSmsMulti(string request, int value, double value2);

    Task<string> SendSmsStr(string request);

    Task<SmsSendResponse> SendSms(SmsSendRequest request);

    Task<SmsMultipleSendResponse> SendMultipleSms(SmsMultipleSendRequest request);

    Task SendSmsNoResponse(SmsSendNoResponseRequest request);

    Task SendSmsNoRequestNoResponse();

    Task SendSmsMultiV(string request, int value, double value2);
}

public class SmsMultipleSendRequest
{
    public IList<string> PhoneNumbers { get; set; }

    public string Message { get; set; }
}

public class SmsMultipleSendResponse
{
    public bool Ok { get; set; }
}

public class SmsSendNoResponseRequest
{
    public string PhoneNumber { get; set; }

    public string Message { get; set; }
}

public class SmsSendRequest
{
    public string PhoneNumber { get; set; }

    public string Message { get; set; }
}

public class SmsSendResponse
{
    public bool Ok { get; set; }
}
```

And it's done :).

Please notice that we put interface definition (and also request and response classes) again in function. To not duplicate the code we can move all definitions
into domain project, compile it into dll and use in client and function.
To use dll in Azure function you have to create bin directory put dll. Then you have to add:

```csharp
#r "{dll name}.dll"
```