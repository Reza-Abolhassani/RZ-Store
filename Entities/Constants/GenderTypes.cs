using System.ComponentModel.DataAnnotations;

namespace Entities.Constants
{
    public enum GenderType
    {
        [Display(Name = "آقا")]
        Male,

        [Display(Name = "خانم")]
        Female
    }
}
