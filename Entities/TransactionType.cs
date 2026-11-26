using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public enum TransactionType
    {
        [Display(Name = "شارژ کیف پول")]
        Deposit = 0,

        [Display(Name = "برداشت از کیف پول")]
        Withdrawal = 1,

        [Display(Name = "برگشت وجه")]
        Refund = 2
    }
}