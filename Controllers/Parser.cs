using Diplom.Enums;
using Diplom.Models;
using Diplom.Repositories;
using Diplom.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Diplom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParserController : ControllerBase
    {
        private readonly PositionStandartsParser _parser;
        private readonly StandardsGraphRepository _standardsRepository;

        public ParserController(PositionStandartsParser parser, StandardsGraphRepository standardsRepository)
        {
            _parser = parser;
            _standardsRepository = standardsRepository;
        }

        [HttpPost("parse_document")]
        public async Task<IActionResult> Post(IFormFile file)
        {
            using MemoryStream ms = new MemoryStream();
            file.CopyTo(ms);
            var res = _parser.ParseDefaultDocument(file.Name, ms);
            await _standardsRepository.SavePositionStandartAsync(res);
            return new JsonResult(res);
        }

        [HttpGet("[action]")]
        public IActionResult LoadFiles()
        {
            return new JsonResult( _parser.LoadFiles());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ParseAndLoadFiles()
        {
            var standardFiles = _parser.LoadFiles();
            var res = new List<PositionStandart>();
            foreach (var standardFile in standardFiles)
            {
                using MemoryStream ms = new MemoryStream(standardFile.Content);

                PositionStandart parsedStandard = null;
                switch (standardFile.Kind)
                {
                    case DocKind.Default:
                        parsedStandard = _parser.ParseDefaultDocument(standardFile.Name, ms);
                        break;
                    case DocKind.Kind2:
                    case DocKind.PdfDefault:
                        break;
                    default: 
                        break;
                }
                if(parsedStandard is not null) res.Add(parsedStandard);
            }

            foreach (var standard in res)
            {
                await _standardsRepository.SavePositionStandartAsync(standard);
            }
            return new JsonResult( res );
        }

        [HttpGet("[action]")]
        public async Task InitializeLevels()
        {
            await _standardsRepository.InitializeOrkLevels();
        }
    }
}
