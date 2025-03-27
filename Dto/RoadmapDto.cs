using Diplom.Constants;

namespace Diplom.Dto
{
    public class RoadmapDto
    {
        public string ProfessionName { get; set; } // Name of the profession
        public OrkLevelRoadmap OrkLevel { get; set; }
    }

    public class OrkLevelRoadmap
    {
        public OrkCvalificationLevelEnum OrkLevel { get; set; } // ORK 4, ORK 5, etc.
        public ProfessionalCardRoadmap PositionCards { get; set; }
        public OrkLevelRoadmap? NextLevel { get; set; }
    }

    public class ProfessionalCardRoadmap
    {
        public List<string> RequiredSkills { get; set; } = new List<string>(); // Required skills
        public List<string> RequiredKnowledge { get; set; } = new List<string>(); // Required knowledge
    }

}
