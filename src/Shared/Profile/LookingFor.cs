using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public enum LookingFor

{
    [Display(Name = "Legeaftaler")]
    Playdates = 0,

    [Display(Name = "Hjemmeundervisnings-grupper")]
    HomeschoolingGroups = 1,

    [Display(Name = "Legeplads ture")]
    PlaygroundTrips = 2,

    [Display(Name = "Mødregrupper")]
    MaternityGroups = 3,

    [Display(Name = "Fædregrupper")]
    PaternityGroups = 4,

    [Display(Name = "Forældre støttegrupper")]
    ParentSupportGroups = 5,

    [Display(Name = "Sportsaktiviteter")]
    SportsActivities = 6,

    [Display(Name = "Kunst og håndværksværksaktiviteter")]
    ArtAndCraftWorkshops = 7,

    [Display(Name = "Musik og danseaktiviteter")]
    MusicAndDanceClasses = 8,

    [Display(Name = "Uddannelsesaktiviteter")]
    EducationalActivities = 9
}
