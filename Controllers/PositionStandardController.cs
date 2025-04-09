using Diplom.Constants;
using Diplom.Dto;
using Diplom.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Diplom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionStandardController : ControllerBase
    {
        private readonly StandardsGraphRepository standardsGraphRepository;

        public PositionStandardController(StandardsGraphRepository standardsGraphRepository)
        {
            this.standardsGraphRepository = standardsGraphRepository;
        }
        [HttpGet()]
        public async Task<IActionResult> GetPositionStandart(string name, OrkCvalificationLevelEnum orkLevel)
        {
            var positionStandart = await standardsGraphRepository.GetPositionStandartByNameAsync(name, orkLevel);
            if (positionStandart == null)
            {
                return NotFound(new { message = "Position Standard not found." });
            }
            return Ok(positionStandart);
        }

        [HttpGet("names")]
        public async Task<ActionResult<List<string>>> GetAllPositionStandardNames()
        {
            var names = await standardsGraphRepository.GetAllPositionStandardNamesAsync();
            if (!names.Any()) return NotFound("No position standards found.");

            return Ok(names);
        }

        [HttpGet("PositionCard/names")]
        public async Task<ActionResult<List<string>>> GetAllPositionCardNames()
        {
            var names = await standardsGraphRepository.GetAllPositionCardNamesAsync();
            if (!names.Any()) return NotFound("No position cards found.");

            return Ok(names);
        }

        [HttpGet("roadmap")]
        public async Task<ActionResult<RoadmapDto>> GetRoadmapForProfession(string positionName)
        {
            var roadmap = await standardsGraphRepository.GetProfessionalRoadmap(positionName);

            return Ok(roadmap);
        }

        [HttpGet("demanded-skills-knowledges")]
        public async Task<DemandedSkillsKnowledgesDto> GetDemandedSkillsKnowledges()
        {
            var skills = await standardsGraphRepository.GetMostInDemandSkills();
            var knowledges = await standardsGraphRepository.GetMostInDemandKnowledges();
            var res = new DemandedSkillsKnowledgesDto
            {
                Skills = skills,
                Knowledges = knowledges
            };
            return res;
        }

        [HttpGet("overlapping-profession")]
        public async Task<List<ProfessionOverlapDto>> GetOverlappingProfession()
        {
            var res = await standardsGraphRepository.GetOverlappingProfessions();
            return res;
        }

        [HttpGet("overlapping-skills-knowledges")]
        public async Task<SkillKnowledgeOverlapByOrkLevel> GetOverlappingSkillsKnowledges(string positionCard)
        {
            var skills = await standardsGraphRepository.GetOverlappingSkillsByPositionCardName(positionCard);
            var knowledges = await standardsGraphRepository.GetOverlappingKnowledgesByPositionCardName(positionCard);
            var res = new SkillKnowledgeOverlapByOrkLevel
            {
                SkillsOverlap = skills,
                KnowledgesOverlap = knowledges
            };
            return res;
        }

        [HttpPost("remaining-skills-knowledges")]
        public async Task<List<RemainingDataDto>> GetRemainingSkillsKnowledges([FromBody] KnownDataDto knownData)
        {
            var res = await standardsGraphRepository.GetRemainingSkillsKnowledges(knownData.PositionCard, knownData.KnownSkills, knownData.KnownKnowledges);
            return res;
        }
    }
}
