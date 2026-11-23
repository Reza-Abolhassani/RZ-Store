using System;

namespace Entities
{
    public class UserAddress : BaseEntity
    {
        // شناسه کاربری که این آدرس متعلق به اوست
        public Guid UserId { get; set; }

        // ارتباط با کاربر
        public User User { get; set; }

        // مشخصات تحویل گیرنده
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }

        // اطلاعات آدرس
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }

        // آیا این آدرس، آدرس پیش‌فرض مشتری است؟
        public bool IsDefault { get; set; }
    }
}