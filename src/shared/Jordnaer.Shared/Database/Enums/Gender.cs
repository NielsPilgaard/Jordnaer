using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum Gender
{
    [Display(Name = "Ikke angivet")]
    NotSet = 0,

    [Display(Name = "Pige")]
    Female = 1,

    [Display(Name = "Dreng")]
    Male = 2,

    [Display(Name = "Ikke-binær")]
    NonBinary = 3,

    [Display(Name = "Andet")]
    Other = 4,
}
