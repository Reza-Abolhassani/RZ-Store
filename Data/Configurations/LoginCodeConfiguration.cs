using Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class LoginCodeConfiguration : IEntityTypeConfiguration<LoginCode>
    {
        public void Configure(EntityTypeBuilder<LoginCode> builder)
        {
            builder.HasIndex(x => x.UserName);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
