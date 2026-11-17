using Entities.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Read);
            builder.HasIndex(x => x.Urgent);
        }
    }
}
