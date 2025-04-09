namespace Diplom.Dto
{
    public class OverlapByOrkLevelsDto
    {
        public string Name { get; set; }
        public int OverlapCount { get; set; }
    }
    public class SkillKnowledgeOverlapByOrkLevel
    {
        public List<OverlapByOrkLevelsDto> SkillsOverlap { get; set; }
        public List<OverlapByOrkLevelsDto> KnowledgesOverlap { get; set; }
    }
}
