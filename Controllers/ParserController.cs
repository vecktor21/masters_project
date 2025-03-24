using Diplom.Enums;
using Diplom.Models;
using Diplom.Repositories;
using Diplom.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
            var res = _parser.ParseKind2Document(file.Name, ms);
            //await _standardsRepository.SavePositionStandartAsync(res);
            return new JsonResult(res);
        }

        [HttpGet("[action]")]
        public IActionResult GetFiles()
        {
            return new JsonResult( _parser.GetFiles());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ParseAndLoadFiles()
        {
            var standardFiles = _parser.GetFiles();
            var res = new List<PositionStandart>();

            Stopwatch timer = new();
            List<long> seconds = new();
            Console.WriteLine("Parse documents: ");
            foreach (var standardFile in standardFiles)
            {
                timer.Start();

                using MemoryStream ms = new MemoryStream(standardFile.Content);

                PositionStandart parsedStandard = null;
                switch (standardFile.Kind)
                {
                    case DocKind.Default:
                        parsedStandard = _parser.ParseDefaultDocument(standardFile.Name, ms);
                        timer.Stop();
                        seconds.Add(timer.ElapsedMilliseconds);
                        Console.WriteLine($"File {standardFile.Name}: {timer.ElapsedMilliseconds}ms");
                        timer.Reset();
                        break;
                    case DocKind.Kind2:
                        parsedStandard = _parser.ParseKind2Document(standardFile.Name, ms);
                        timer.Stop();
                        seconds.Add(timer.ElapsedMilliseconds);
                        Console.WriteLine($"File {standardFile.Name}: {timer.ElapsedMilliseconds}ms");
                        timer.Reset();
                        break;
                    case DocKind.PdfDefault:
                        break;
                    default: 
                        break;
                }
                if(parsedStandard is not null) res.Add(parsedStandard);
            }
            Console.WriteLine($"Avreage time: {seconds.Average()}; Minimal time: {seconds.Min()}; Maximum time: {seconds.Max()}");
            seconds.Clear();
            Console.WriteLine("Load documents: ");
            foreach (var standard in res)
            {
                timer.Start();
                await _standardsRepository.SavePositionStandartAsync(standard);
                timer.Stop();
                seconds.Add(timer.ElapsedMilliseconds);
                Console.WriteLine($"Standard {standard.Name}: {timer.ElapsedMilliseconds}ms");
                timer.Reset();
            }
            Console.WriteLine($"Avreage time: {seconds.Average()}; Minimal time: {seconds.Min()}; Maximum time: {seconds.Max()}");
            return new JsonResult( res );
        }


        [HttpGet("[action]")]
        public IActionResult ParseFiles()
        {
            var standardFiles = _parser.GetFiles();
            var res = new List<PositionStandart>();


            Stopwatch timer = new();
            List<long> seconds = new();

            foreach (var standardFile in standardFiles)
            {
                timer.Start();

                using MemoryStream ms = new MemoryStream(standardFile.Content);
                PositionStandart parsedStandard = null;
                switch (standardFile.Kind)
                {
                    case DocKind.Default:
                        parsedStandard = _parser.ParseDefaultDocument(standardFile.Name, ms);
                        timer.Stop();
                        seconds.Add(timer.ElapsedMilliseconds);
                        Console.WriteLine($"File {standardFile.Name}: {timer.ElapsedMilliseconds}ms");
                        timer.Reset();
                        break;
                    case DocKind.Kind2:
                        parsedStandard = _parser.ParseKind2Document(standardFile.Name, ms);
                        timer.Stop();
                        seconds.Add(timer.ElapsedMilliseconds);
                        Console.WriteLine($"File {standardFile.Name}: {timer.ElapsedMilliseconds}ms");
                        timer.Reset();
                        break;
                    case DocKind.PdfDefault:
                        break;
                    default:
                        break;
                }
                if (parsedStandard is not null) res.Add(parsedStandard);
            }

            Console.WriteLine($"Avreage time: {seconds.Average()}; Minimal time: {seconds.Min()}; Maximum time: {seconds.Max()}");

            return new JsonResult(res);
        }

        [HttpGet("[action]")]
        public async Task InitializeLevels()
        {
            await _standardsRepository.InitializeOrkLevels();
        }
    }
}
