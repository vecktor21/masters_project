using Diplom.Constants;
using Diplom.Dto;
using Diplom.Enums;
using Diplom.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace Diplom.Services
{
    public class PositionStandartsParser
    {
        public List<LoadedFileDto> LoadFiles()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Standarts");
            List<LoadedFileDto> result = new();

            var fileNames = Directory.GetFiles(path);
            var enumKeys = Enum.GetNames(typeof(DocKind));

            foreach (var fileName in fileNames)
            {
                var separated = fileName.Split(Path.DirectorySeparatorChar);
                var fileNameKind = separated[separated.Length - 1].Split(" ")[0];
                if (enumKeys.Contains(fileNameKind))
                {
                    result.Add(new LoadedFileDto
                    {
                        Kind = (DocKind) Enum.Parse(typeof(DocKind), fileNameKind, true),
                        Content = File.ReadAllBytes(fileName),
                        Name = separated[separated.Length - 1]
                    });
                }
            }
            return result;
        }

        public PositionStandart ParseDefaultDocument(string fileName, MemoryStream ms)
        {
            PositionStandart standart = new PositionStandart();

            using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                var table = body.Elements<Table>().FirstOrDefault();

                if (table != null)
                {
                    var rows = table.Elements<TableRow>().ToList();

                    // Extracting Name, StandartDevelopmentGoal, StandartDescription
                    int passportIndex = rows.FindIndex(r => r.InnerText.ToLower().Contains("Паспорт профессионального стандарта".ToLower()));
                    if (passportIndex != -1)
                    {
                        standart.Name = GetCellText(rows, passportIndex + 1);
                        standart.StandartDevelopmentGoal = GetCellText(rows, passportIndex + 2);
                        standart.StandartDescription = GetCellText(rows, passportIndex + 3);
                    }
                    else
                    {

                    }

                    // Extracting General Info
                    int generalInfoIndex = rows.FindIndex(r => r.InnerText.Contains("Общие положения"));
                    if (generalInfoIndex != -1)
                    {
                        standart.GeneralInfo = GetCellText(rows, generalInfoIndex + 1);
                    }

                    // Parse Position Cards
                    for (int i = 0; i < rows.Count; i++)
                    {
                        try
                        {
                            if (rows[i].InnerText.Contains("КАРТОЧКА ПРОФЕССИИ"))
                            {
                                PositionCard card = new PositionCard();

                                // Relative indices for the card details
                                card.Code = GetCellText(rows, i + 1, 1);
                                card.Name = GetCellText(rows, i + 2, 1);
                                var orkLevelText = GetCellText(rows, i + 3, 1);
                                if(orkLevelText is not null 
                                    && orkLevelText != "-"
                                    && int.TryParse(orkLevelText[0].ToString(), out int level)
                                    )
                                {
                                    card.OrkCvalificationLevel = (OrkCvalificationLevelEnum) level;
                                }
                                card.KsCvalificationLevel = GetCellText(rows, i + 4, 1);

                                // Parse Position Functions starting after "Трудовая функция"
                                for (int j = i + 3; j < rows.Count; j++)
                                {
                                    try
                                    {
                                        if (rows[j].InnerText.Contains("Трудовая функция"))
                                        {
                                            PositionFunctions function = new PositionFunctions();
                                            function.FunctionName = ExtractFunctionName(rows[j]);

                                            if(function.FunctionName.Contains("Трудовая функция"))
                                            {
                                                function.FunctionName = Regex.Replace(function.FunctionName, @"Трудовая функция \d+", "").Trim();
                                            }

                                            // Extract Skills and Knowledges
                                            function.Skills = ExtractListItems(rows, j).Where(x=> x != "").ToList();
                                            function.Knowledges = ExtractListItems(rows, j + 1).Where(x => x != "").ToList();

                                            card.Functions.Add(function);
                                        }
                                        else if (rows[j].InnerText.Contains("КАРТОЧКА ПРОФЕССИИ"))
                                        {
                                            break;  // Stop when the next "КАРТОЧКА ПРОФЕССИИ" starts
                                        }
                                    }
                                    catch (IndexOutOfRangeException ex)
                                    {
                                        Console.WriteLine($"Error while parsing Position Functions. File {fileName}. Line {j}");
                                        Console.WriteLine(ex.Message);
                                        throw;
                                    }
                                }

                                standart.Cards.Add(card);
                            }
                        }
                        catch(IndexOutOfRangeException ex)
                        {
                            Console.WriteLine($"Error while parsing Proffession card. File {fileName}. Line {i}");
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }
                }
            }

            return standart;
        }

        private string GetCellText(List<TableRow> rows, int rowIndex)
        {
            return rowIndex < rows.Count ? rows[rowIndex].InnerText.Trim() : string.Empty;
        }

        private string GetCellText(List<TableRow> rows, int rowIndex, int column)
        {
            var row = rows[rowIndex];
            var cellElements = row.Elements<TableCell>().ToList();
            var cell = cellElements[column];
            
            if(rowIndex < rows.Count)
            {
                var s = cell.Elements<Paragraph>()
               .ToList()
               .Select(line =>
               {
                   var line_selected = line.Elements().Skip(1).Select(s => s.InnerText).ToList();

                   if (line_selected.Count > 1)
                   {

                   }

                   var s2 = line_selected.Aggregate((res, cur) => res += cur == "" ? " " : cur.Replace("\n", "")).Trim();
                   return s2;
               })
               .Aggregate((res, cur) => res += " " + cur.Replace("\n", ""))
               .Trim()
               .Replace("  "," ");
                return s;
            }
            
            return string.Empty;
        }

        private string ExtractFunctionName(TableRow row)
        {
            var elements = row.Elements<TableCell>().First().Elements<Paragraph>().ToList();
            var text = elements.Count == 1 ? elements.First().InnerText : elements[1].InnerText;
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Count() == 0)
            {
                text = elements[0].InnerText;
                lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            return lines[0].Trim();
        }

        private List<string> ExtractListItems(List<TableRow> rows, int rowIndex)
        {
            var list = new List<string>();
            if (rowIndex < rows.Count)
            {
                var cells = rows[rowIndex].Elements<TableCell>().ToList();
                if (cells.Count > 1)
                {
                    list = cells[1].Elements<Paragraph>().Skip(1).ToList()
                        //.Where(line => !line.Contains("Умения и навыки") && !line.Contains("Знания"))
                        .Select(line => {
                            var line_selected = line.Elements().Skip(1).Select(s => s.InnerText).ToList();

                            if(line_selected.Count < 1)
                            {
                                return String.Empty;
                            }

                            var s2 = line_selected.Aggregate((res, cur) => res += cur == "" ? " " : cur.Replace("\n","")).Trim();
                            return s2;
                        })
                        .Distinct()
                        .ToList();
                }
            }
            return list;
        }


    }
}
