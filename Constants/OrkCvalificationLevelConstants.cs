﻿using System.ComponentModel.DataAnnotations;

namespace Diplom.Constants
{
    public enum OrkCvalificationLevelEnum
    {
        [Display(Name = "Техническое и профессиональное образование, без практического опыта")]
        Level4=4,
        [Display(Name = "Высшее образование, дополнительные профессиональные образовательные программы, без практического опыта")]
        Level5=5,
        [Display(Name = "Высшее образование, практический опыт")]
        Level6=6,
        [Display(Name = "Концептуальные профессиональные и/или научные знания (в том числе и инновационных) и опыта в определенной области и/или на стыке областей")]
        Level7 = 7
    }
}
