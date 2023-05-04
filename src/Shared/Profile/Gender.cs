using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public enum Gender
{
    [Display(Name = "Foretrækker ikke at sige")]
    PreferNotToSay = 0,

    [Display(Name = "Hun")]
    Female = 1,

    [Display(Name = "Han")]
    Male = 2,

    [Display(Name = "Ikke-binær")]
    NonBinary = 3,

    [Display(Name = "Andet")]
    Other = 4,
}
