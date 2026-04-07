using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Billing.Api.Data.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Last4).HasMaxLength(4).IsRequired();
        builder.Property(e => e.CardBrand).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AuthorizeNetPaymentProfileId).HasMaxLength(200);
        builder.HasIndex(e => e.OrganizationId);
    }
}
