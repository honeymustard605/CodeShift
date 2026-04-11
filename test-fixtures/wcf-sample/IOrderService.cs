using System.ServiceModel;
using System.Runtime.Serialization;

[ServiceContract(Namespace = "http://legacy.corp/OrderService")]
public interface IOrderService
{
    [OperationContract]
    OrderResponse GetOrder(int orderId);

    [OperationContract]
    void SubmitOrder(OrderRequest request);
}

[DataContract]
public class OrderRequest
{
    [DataMember] public int CustomerId { get; set; }
    [DataMember] public string ProductCode { get; set; }
    [DataMember] public int Quantity { get; set; }
}

[DataContract]
public class OrderResponse
{
    [DataMember] public int OrderId { get; set; }
    [DataMember] public string Status { get; set; }
    [DataMember] public DateTime CreatedAt { get; set; }
}
