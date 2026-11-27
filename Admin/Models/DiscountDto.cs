using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebFramework.Api;

namespace Admin.Models
{
    public class DiscountDto : BaseDto<DiscountDto, Discount>, IValidatableObject
    {
        [Required(ErrorMessage = "کد تخفیف را وارد کنید")]
        public string Code { get; set; }

        [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین صفر تا صد باشد")]
        public int Percentage { get; set; }

        [Range(0L, long.MaxValue, ErrorMessage = "مبلغ تخفیف معتبر نیست")]
        public long Amount { get; set; }

        public DateTime Startdate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Startdate == DateTime.MinValue)
            {
                yield return new ValidationResult(
                    "تاریخ شروع را وارد کنید",
                    new[] { nameof(Startdate) });
            }

            if (EndDate == DateTime.MinValue)
            {
                yield return new ValidationResult(
                    "تاریخ پایان را وارد کنید",
                    new[] { nameof(EndDate) });
            }

            if (Startdate != DateTime.MinValue &&
                EndDate != DateTime.MinValue &&
                EndDate <= Startdate)
            {
                yield return new ValidationResult(
                    "تاریخ پایان باید بعد از تاریخ شروع باشد",
                    new[] { nameof(EndDate) });
            }

            if (Percentage == 0 && Amount == 0)
            {
                yield return new ValidationResult(
                    "درصد یا مبلغ تخفیف را وارد کنید",
                    new[] { nameof(Percentage), nameof(Amount) });
            }

            if (Percentage > 0 && Amount > 0)
            {
                yield return new ValidationResult(
                    "تخفیف باید یا درصدی باشد یا مبلغ ثابت؛ هر دو مجاز نیستند",
                    new[] { nameof(Percentage), nameof(Amount) });
            }
        }
    }
}



