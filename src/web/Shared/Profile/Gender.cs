using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public enum Gender
{
    [Display(Name = "Ønsker ikke at oplyse")]
    PreferNotToSay = 0,

    [Display(Name = "Pige")]
    Female = 1,

    [Display(Name = "Dreng")]
    Male = 2,

    [Display(Name = "Ikke-binær")]
    NonBinary = 3,

    [Display(Name = "Andet")]
    Other = 4,
}
