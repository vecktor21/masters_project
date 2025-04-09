namespace Diplom.Dto
{
    public class DemandDto
    {
        public string Name { get; set; } = null!;
        public int Demand { get; set; }
    }

    public class DemandedSkillsKnowledgesDto
    {
        public List<DemandDto> Skills { get; set; } = new();
        public List<DemandDto> Knowledges { get; set; } = new();
    }
}
