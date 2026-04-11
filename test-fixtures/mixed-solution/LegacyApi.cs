// Mixed solution: .NET Framework 4.7.2 C# Web API alongside WCF service reference
using System;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.ServiceModel;

[RoutePrefix("api/legacy")]
public class LegacyApiController : ApiController
{
    [HttpGet, Route("status")]
    public IHttpActionResult GetStatus()
    {
        // Blocking call — should become async
        Thread.Sleep(100);

        var user = HttpContext.Current.User.Identity.Name;
        return Ok(new { status = "ok", user });
    }

    [HttpPost, Route("orders")]
    public IHttpActionResult PostOrder(OrderPayload payload)
    {
        // WCF proxy call
        var factory = new ChannelFactory<IOrderService>(
            new BasicHttpBinding(),
            new EndpointAddress("http://internal/OrderService.svc"));
        var channel = factory.CreateChannel();
        channel.SubmitOrder(new OrderRequest
        {
            CustomerId = payload.CustomerId,
            ProductCode = payload.ProductCode,
            Quantity = payload.Quantity
        });
        return Ok();
    }
}

public class OrderPayload
{
    public int CustomerId { get; set; }
    public string ProductCode { get; set; }
    public int Quantity { get; set; }
}
