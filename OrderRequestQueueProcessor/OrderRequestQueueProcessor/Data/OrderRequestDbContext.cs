using Microsoft.EntityFrameworkCore;
using OrderRequestQueueProcessor.Models;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Data
{
    public class OrderRequestDbContext : DbContext
    {
        public OrderRequestDbContext(DbContextOptions<OrderRequestDbContext> options)
        : base(options)
        {
        }

        public DbSet<OrderRequestDto> OrderRequests { get; set; }
        public DbSet<OrderRequestLineDto> OrderRequestLines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // If you're using fluent configs in separate classes
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderRequestDbContext).Assembly);

            modelBuilder.Entity<OrderRequestDto>(entity =>
            {
                entity.ToTable("ORDER_REQUESTS");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("ORQ_ID")
                      .HasColumnType("NUMBER(38,0)")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.PortalRequestId)
                        .HasColumnName("ORQ_PORTAL_REQUEST_ID");
                entity.Property(e => e.AccountId)
                        .HasColumnName("ORQ_IA_INVOICE_ACCOUNT");
                entity.Property(e => e.DeliveryDate)
                        .HasColumnName("ORQ_DELIVERY_DATE");
                entity.Property(e => e.TownId)
                        .HasColumnName("ORQ_TWN_REFERENCE");
                entity.Property(e => e.DateEntered)
                        .HasColumnName("ORQ_WHEN_ENTERED");
                entity.Property(e => e.ContactId)
                        .HasColumnName("ORQ_CT_ID");
                entity.Property(e => e.CustomerOrderNo)
                        .HasColumnName("ORQ_CUST_ORDER_NO");
                entity.Property(e => e.DepotId)
                        .HasColumnName("ORQ_DEPOT_ID");
                entity.Property(e => e.Time)
                        .HasColumnName("ORQ_TIME");
                entity.Property(e => e.Status)
                        .HasColumnName("ORQ_STATUS");
                entity.Property(e => e.OrderId)
                        .HasColumnName("ORQ_SO_INT_ORDER_REF");

                entity.Property(e => e.SiteContactId)
                        .HasColumnName("ORQ_SITE_CONTACT_ID");

                entity.HasMany(e => e.OrderRequestLines)
                      .WithOne()
                      .HasForeignKey(l => l.OrderRequestId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderRequestLineDto>(entity =>
            {
                entity.ToTable("ORDER_REQUEST_LINES");
                entity.HasKey(e => new { e.OrderRequestId, e.LineNo });

                entity.Property(e => e.OrderRequestId)
                      .HasColumnName("ORQL_ORQ_ID")
                        .HasColumnType("NUMBER(38,0)");
                entity.Property(e => e.LineNo)
                        .HasColumnName("ORQL_LINE_NO");
                entity.Property(e => e.ProductId)
                        .HasColumnName("ORQL_PRODUCT_CODE");
                entity.Property(e => e.RateType)
                        .HasColumnName("ORQL_DELIVERY_RATE_TYPE");
                entity.Property(e => e.Quantity)
                        .HasColumnName("ORQL_QUANTITY");
                entity.Property(e => e.Identifier)
                        .HasColumnName("ORQL_IDENTIFIER")
                        .HasDefaultValue(string.Empty);
                entity.Property(e => e.Position)
                        .HasColumnName("ORQL_POSITION");


            });
        }
    }
}
