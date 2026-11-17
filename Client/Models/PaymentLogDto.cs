using Entities.Payments;

using System;

using WebFramework.Api;

namespace Admin.Models.Payments
{
    public class PaymentLogDto : BaseDto<PaymentLogDto, PaymentLog>
    {
        public Guid PatientUserId { get; set; }
        public string PatientUserFirstName { get; set; }
        public string PatientUserLastName { get; set; }
        public string RefId { get; set; }
        public string Description { get; set; }
        public long TotalPrice { get; set; }
    }
}
