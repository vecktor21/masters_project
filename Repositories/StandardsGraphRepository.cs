using Diplom.Constants;
using Diplom.Dto;
using Diplom.Models;
using Diplom.Options;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using Neo4j.Driver;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Diplom.Repositories
{
    public class StandardsGraphRepository : IDisposable
    {
        private readonly IDriver _driver;

        public StandardsGraphRepository(IOptions<Neo4jSettings> opt)
        {
            _driver = GraphDatabase.Driver(opt.Value.Uri, AuthTokens.Basic(opt.Value.Login, opt.Value.Password));
        }

        public async Task SavePositionStandartAsync(PositionStandart standart)
        {
            await using var session = _driver.AsyncSession();
#pragma warning disable CS0618 // Type or member is obsolete
            await session.WriteTransactionAsync(async tx =>
            {
                var existsLevelsQuery = $"MATCH (l:OrkLevel) WITH count(l) > 0 AS EXISTS_LEVELS return EXISTS_LEVELS";
                var existsLevelsQueryCursor = await tx.RunAsync(existsLevelsQuery);
                var existsLevelsQueryResult = await existsLevelsQueryCursor.ToListAsync();
                if((bool?)existsLevelsQueryResult.FirstOrDefault()?.FirstOrDefault().Value == false)
                {
                    await InitializeOrkLevels();
                }

                // Create or merge PositionStandart node and get its ID
                var createStandartQuery = @"
                MERGE (ps:PositionStandart {Name: $name})
                ON CREATE SET ps.StandartDevelopmentGoal = $goal, 
                              ps.StandartDescription = $description, 
                              ps.GeneralInfo = $info
                RETURN ps.Name";

                var result = await tx.RunAsync(createStandartQuery, new
                {
                    name = standart.Name,
                    goal = standart.StandartDevelopmentGoal,
                    description = standart.StandartDescription,
                    info = standart.GeneralInfo
                });
                var s3 = await result.ToListAsync();

                // Create related PositionCard nodes and connect them to PositionStandart
                foreach (var card in standart.Cards)
                {
                    var createCardQuery = @"
                    MERGE (c:PositionCard {Name: $name, KsCvalificationLevel: $ksLevel})
                    ON CREATE SET c.Code = $code
                    WITH c
                    MATCH (ps:PositionStandart {Name: $psName}),
                          (l:OrkLevel {Level: $orkLevel})
                    MERGE (ps)-[:HAS_CARD]->(c)
                    MERGE (c)-[:HAS_ORK_LEVEL]->(l)
                    return ps.Name, c.Name, l";

                    var cardRes = await tx.RunAsync(createCardQuery, new
                    {
                        code = card.Code,
                        name = card.Name,
                        orkLevel = (int)card.OrkCvalificationLevel,
                        ksLevel = card.KsCvalificationLevel,
                        psName = standart.Name,
                    });
                    var s2 = await cardRes.ToListAsync();

                    // Create related PositionFunctions nodes and connect them to PositionCard
                    foreach (var function in card.Functions)
                    {
                        var createFunctionQuery = @"
                        MERGE (f:PositionFunction {FunctionName: $functionName})
                        WITH f
                        MATCH (c:PositionCard {Name: $cardName, KsCvalificationLevel: $ksLevel}),
                            (c)-[:HAS_ORK_LEVEL]->(l:OrkLevel{Level:$orkLevel})
                        MERGE (c)-[hf:HAS_FUNCTION{OrkLevel:$orkLevel}]->(f) 
                        ON CREATE SET hf.OrkLevel = $orkLevel
                        ON MATCH SET hf.OrkLevel = $orkLevel
                        return f.FunctionName, c, l";

                        var functionResult = await tx.RunAsync(createFunctionQuery, new
                        {
                            cardName = card.Name,
                            functionName = function.FunctionName,
                            orkLevel = (int)card.OrkCvalificationLevel,
                            ksLevel = card.KsCvalificationLevel,
                        });
                        var s = await functionResult.ToListAsync();

                        // Create Skill nodes and connect them to PositionFunction
                        foreach (var skill in function.Skills)
                        {
                            var createSkillQuery = @"
                            MERGE (s:Skill {Name: $skill})
                            WITH s
                            MATCH (f:PositionFunction {FunctionName: $functionName})
                            MERGE (f)-[r:REQUIRES_SKILL{OrkLevel:$level}]->(s)
                            ON CREATE SET r.OrkLevel=$level
                            return s";

                            var skillRes = await tx.RunAsync(createSkillQuery, new
                            {
                                skill,
                                functionName = function.FunctionName,
                                level = (int)card.OrkCvalificationLevel
                            });
                            var k = await skillRes.ToListAsync();
                        }

                        // Create Knowledge nodes and connect them to PositionFunction
                        foreach (var knowledge in function.Knowledges)
                        {
                            var createKnowledgeQuery = @"
                            MERGE (k:Knowledge {Name: $knowledge})
                            WITH k
                            MATCH (f:PositionFunction {FunctionName: $functionName})
                            MERGE (f)-[r:REQUIRES_KNOWLEDGE{OrkLevel:$level}]->(k) 
                            ON CREATE SET r.OrkLevel=$level
                            return k";

                            var knowledgeRes = await tx.RunAsync(createKnowledgeQuery, new
                            {
                                knowledge,
                                functionName = function.FunctionName,
                                level = (int)card.OrkCvalificationLevel
                            });
                            var k = await knowledgeRes.ToListAsync();
                        }
                    }
                }
            });
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task InitializeOrkLevels()
        {
            await using var session = _driver.AsyncSession();
#pragma warning disable CS0618 // Type or member is obsolete
            await session.WriteTransactionAsync(async tx =>
            {
                var levels = Enum.GetValues<OrkCvalificationLevelEnum>();
                foreach (var level in levels)
                {
                    var createLevelQuery = @"
                        MERGE (l:OrkLevel {Level: $level})
                        ON MATCH SET l.Name=$name
                        ON CREATE SET l.Name=$name
                        RETURN l.Name";
                    var type = level.GetType();
                    var field = type.GetField(level.GetDisplayName());
                    var attributes = field.GetCustomAttribute<DisplayAttribute>();

                    var result = await tx.RunAsync(createLevelQuery, new
                    {
                        level = (int)level,
                        name = attributes.Name
                    });
                    var s = await result.ToListAsync();
                }
            });
        }

        public async Task<PositionStandartDto?> GetPositionStandartByNameAsync(string positionStandartName,
            OrkCvalificationLevelEnum orkLevel)
        {
            await using var session = _driver.AsyncSession();

            var query = @"
            MATCH (ps:PositionStandart {Name: $name})
            OPTIONAL MATCH (ps)-[:HAS_CARD]->(card:PositionCard)
            MATCH (card)-[:HAS_ORK_LEVEL]->(ork:OrkLevel {Level: $level})
            OPTIONAL MATCH (card)-[:HAS_FUNCTION {OrkLevel: ork.Level}]->(func:PositionFunction)
            OPTIONAL MATCH (func)-[:REQUIRES_SKILL {OrkLevel: ork.Level}]->(skill:Skill)
            OPTIONAL MATCH (func)-[:REQUIRES_KNOWLEDGE {OrkLevel: ork.Level}]->(knowledge:Knowledge)
            RETURN ps, card, ork, func, skill, knowledge";

            var result = await session.RunAsync(query, new { name = positionStandartName, level = (int)orkLevel });

            var positionStandart = new PositionStandartDto();
            var positionCards = new Dictionary<string, PositionCardDto>();
            var positionFunctions = new Dictionary<string, PositionFunctionDto>();

            await foreach (var record in result)
            {
                // Populate PositionStandart
                if (positionStandart.Name == null)
                {
                    var ps = record["ps"].As<INode>();
                    positionStandart = new PositionStandartDto
                    {
                        Name = ps["Name"].As<string>(),
                        StandartDevelopmentGoal = ps["StandartDevelopmentGoal"].As<string>(),
                        StandartDescription = ps["StandartDescription"].As<string>(),
                        GeneralInfo = ps["GeneralInfo"].As<string>()
                    };
                }

                // Process PositionCard
                if (record["card"] is INode cardNode)
                {
                    var cardName = cardNode["Name"].As<string>();
                    if (!positionCards.ContainsKey(cardName))
                    {
                        positionCards[cardName] = new PositionCardDto
                        {
                            Name = cardName,
                            Code = cardNode["Code"].As<string>(),
                            KsCvalificationLevel = cardNode["KsCvalificationLevel"].As<string>()
                        };
                    }

                    // Check if OrkLevel node exists before accessing it
                    if (record["ork"] is INode orkNode)
                    {
                        positionCards[cardName].OrkCvalificationLevel = orkNode["Level"].As<string>();
                    }

                    // Process PositionFunction
                    if (record["func"] is INode funcNode)
                    {
                        var functionName = funcNode["FunctionName"].As<string>();
                        if (!positionFunctions.ContainsKey(functionName))
                        {
                            positionFunctions[functionName] = new PositionFunctionDto
                            {
                                FunctionName = functionName
                            };
                        }

                        // Process Skills
                        if (record["skill"] is INode skillNode)
                        {
                            var skillName = skillNode["Name"].As<string>();
                            if (!positionFunctions[functionName].Skills.Contains(skillName))
                            {
                                positionFunctions[functionName].Skills.Add(skillName);
                            }
                        }

                        // Process Knowledges
                        if (record["knowledge"] is INode knowledgeNode)
                        {
                            var knowledgeName = knowledgeNode["Name"].As<string>();
                            if (!positionFunctions[functionName].Knowledges.Contains(knowledgeName))
                            {
                                positionFunctions[functionName].Knowledges.Add(knowledgeName);
                            }
                        }

                        positionCards[cardName].Functions.Add(positionFunctions[functionName]);
                    }
                }
            }

            positionStandart.Cards = positionCards.Values.ToList();
            return positionStandart;
        }

        public async Task<List<string>> GetAllPositionStandardNamesAsync()
        {
            var names = new List<string>();

            var query = "MATCH (ps:PositionStandart) RETURN ps.Name AS Name";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query);

            await result.ForEachAsync(record =>
            {
                names.Add(record["Name"].As<string>());
            });

            return names;
        }


        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}