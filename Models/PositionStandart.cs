using Diplom.Constants;

namespace Diplom.Models
{
    public class PositionStandart
    {
        public string Name { get; set; }
        public string StandartDevelopmentGoal { get; set; }
        public string StandartDescription { get; set; }
        public string GeneralInfo { get; set; }
        public List<PositionCard> Cards { get; set; } = new List<PositionCard>();
    }

    public class PositionCard
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public OrkCvalificationLevelEnum OrkCvalificationLevel { get; set; }
        public string KsCvalificationLevel { get; set; }
        public List<PositionFunctions> Functions { get; set; } = new List<PositionFunctions>();
    }

    public class PositionFunctions
    {
        public string FunctionName { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> Knowledges { get; set; } = new List<string>();
    }

}
