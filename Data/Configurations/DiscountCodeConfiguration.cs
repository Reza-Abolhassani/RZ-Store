using Entities.Setting;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
    {
        public void Configure(EntityTypeBuilder<DiscountCode> builder)
        {
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.DueDate);
            builder.HasIndex(x => x.UserId);
        }
    }
}
