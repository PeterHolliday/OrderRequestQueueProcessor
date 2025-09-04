using Newtonsoft.Json;

namespace OrderRequestQueueProcessor.Models
{
    public class OrderRequestPayloadDto
    {
        [JsonProperty("order_request")]
        public OrderRequestDto Order_Request { get; set; } = new();
    }
}
