using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Models
{
    public class OrderRequestDto
    {
        [JsonProperty("orq_id")]
        public decimal Id { get; set; }  // ORQ_ID

        [JsonProperty("orq_portal_request_id")]
        public int? PortalRequestId { get; set; }  // ORQ_PORTAL_REQUEST_ID

        [JsonProperty("orq_ia_invoice_account")]
        public int? AccountId { get; set; }  // ORQ_IA_INVOICE_ACCOUNT

        [JsonProperty("orq_delivery_date")]
        public DateTime? DeliveryDate { get; set; }  // ORQ_DELIVERY_DATE

        [JsonProperty("orq_twn_reference")]
        public int? TownId { get; set; }  // ORQ_TWN_REFERENCE

        [JsonProperty("orq_when_entered")]
        public DateTime? DateEntered { get; set; }  // ORQ_WHEN_ENTERED

        [JsonProperty("orq_ct_id")]
        public int? ContactId { get; set; }  // ORQ_CT_ID

        [JsonProperty("orq_cust_order_no")]
        public string? CustomerOrderNo { get; set; }  // ORQ_CUST_ORDER_NO

        [JsonProperty("orq_depot_id")]
        public int? DepotId { get; set; }  // ORQ_DEPOT_ID

        [JsonProperty("orq_time")]
        public string? Time { get; set; }  // ORQ_TIME

        [JsonProperty("orq_status")]
        public string? Status { get; set; }  // ORQ_STATUS

        [JsonProperty("orq_so_int_order_ref")]
        public int? OrderId { get; set; }  // ORQ_SO_INT_ORDER_REF

        [JsonProperty("orq_site_contact_id")]
        public int? SiteContactId { get; set; }  // ORQ_SITE_CONTACT_ID

        [NotMapped]
        [JsonProperty("order_request_lines")]
        public List<OrderRequestLineDto> OrderRequestLines { get; set; } = new List<OrderRequestLineDto>();
    }
}
