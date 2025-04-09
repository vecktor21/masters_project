namespace Diplom.Dto
{
    public class ProfessionOverlapDto
    {
        public string Position1 { get; set; } = null!;
        public string Position2 { get; set; } = null!;
        public int FunctionOverlapCount { get; set; }
        public int SkillOverlapCount { get; set; }
        public int KnowledgeOverlapCount { get; set; }
        public int TotalOverlap { get; set; }
    }
}
