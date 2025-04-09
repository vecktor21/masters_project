namespace Diplom.Dto
{
    public class RemainingDataDto
    {
        public int OrkLevel { get; set; }
        public List<string> RemainingSkills { get; set; }
        public List<string> RemainingKnowledges { get; set; }
    }
    public class KnownDataDto
    {
        public string PositionCard { get; set; }
        public string[] KnownSkills { get; set; }
        public string[] KnownKnowledges { get; set; }
    }
}
