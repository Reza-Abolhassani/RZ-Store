using System.ComponentModel.DataAnnotations;

namespace Entities.Constants
{
    public enum SystemRoles
    {
        [Display(Name = "Admin")]
        Admin,
        [Display(Name = "Client")]
        Client,
    }

    public enum TicketStatus
    {
        [Display(Name = "در انتظار کارشناس")]
        Pending,
        [Display(Name = "اتمام رسیده")]
        Closed,
        [Display(Name = "در حال رسیدگی")]
        Waiting,
    }
}
