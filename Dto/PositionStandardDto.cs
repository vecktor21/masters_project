namespace Diplom.Dto
{
    public class PositionStandartDto
    {
        public string Name { get; set; }
        public string StandartDevelopmentGoal { get; set; }
        public string StandartDescription { get; set; }
        public string GeneralInfo { get; set; }
        public List<PositionCardDto> Cards { get; set; } = new List<PositionCardDto>();
    }

    public class PositionCardDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string OrkCvalificationLevel { get; set; } // Enum converted to string
        public string KsCvalificationLevel { get; set; }
        public List<PositionFunctionDto> Functions { get; set; } = new List<PositionFunctionDto>();
    }

    public class PositionFunctionDto
    {
        public string FunctionName { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> Knowledges { get; set; } = new List<string>();
    }

}
