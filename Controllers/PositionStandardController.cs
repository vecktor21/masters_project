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
    }
}
