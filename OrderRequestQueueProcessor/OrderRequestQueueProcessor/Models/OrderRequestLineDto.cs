using Newtonsoft.Json;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Models
{
    public class OrderRequestLineDto
    {
        [JsonProperty("orql_orq_id")]
        public decimal OrderRequestId { get; set; }  // ORQL_ORQ_ID

        [JsonProperty("orql_line_no")]
        public int? LineNo { get; set; }  // ORQL_LINE_NO

        [JsonProperty("orql_product_code")]
        public string? ProductId { get; set; }  // ORQL_PRODUCT_CODE

        [JsonProperty("orql_delivery_rate_type")]
        public string? RateType { get; set; }  // ORQL_DELIVERY_RATE_TYPE

        [JsonProperty("orql_quantity")]
        public decimal? Quantity { get; set; }  // ORQL_QUANTITY

        [JsonProperty("orql_identifier")]
        public string? Identifier { get; set; } = string.Empty;

        [JsonProperty("orql_position")]
        public decimal? Position { get; set; }
    }
}
