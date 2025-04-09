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

        public async Task<List<string>> GetAllPositionCardNamesAsync()
        {
            var names = new List<string>();

            var query = "MATCH (ps:PositionCard) RETURN ps.Name AS Name";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query);

            await result.ForEachAsync(record =>
            {
                names.Add(record["Name"].As<string>());
            });

            return names;
        }


        public async Task<RoadmapDto> GetProfessionalRoadmap(string professionName)
        {
            var query = @"
        MATCH (pc:PositionCard {Name: $professionName})-[:HAS_FUNCTION]->(pf:PositionFunction)
        MATCH (pc)-[:HAS_ORK_LEVEL]->(ork:OrkLevel)
        OPTIONAL MATCH (pf)-[:REQUIRES_SKILL {OrkLevel: ork.Level}]->(s:Skill)
        OPTIONAL MATCH (pf)-[:REQUIRES_KNOWLEDGE {OrkLevel: ork.Level}]->(k:Knowledge)
        RETURN pc, ork.Level AS OrkLevel, 
               COLLECT(DISTINCT s.Name) AS Skills, 
               COLLECT(DISTINCT k.Name) AS Knowledge
        ORDER BY OrkLevel ASC";


            var roadmap = new RoadmapDto { ProfessionName = professionName };
            OrkLevelRoadmap? previousLevel = null;
            OrkLevelRoadmap? firstLevel = null;

            using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query, new { professionName });

            await foreach (var record in result)
            {
                int orkLevelValue = record["OrkLevel"].As<int>();

                // Create PositionCard
                var pcNode = record["pc"].As<INode>();
                var positionCard = new ProfessionalCardRoadmap
                {
                    //PositionCardName = pcNode["Name"].As<string>(),
                    //Code = pcNode["Code"].As<string>(),
                    RequiredSkills = record["Skills"].As<List<string>>(),
                    RequiredKnowledge = record["Knowledge"].As<List<string>>()
                };

                // Create ORK Level Roadmap
                var orkLevel = new OrkLevelRoadmap
                {
                    OrkLevel = (OrkCvalificationLevelEnum)orkLevelValue,
                    PositionCards = positionCard // Assign single object
                };

                // Link previous level to current one
                if (previousLevel != null)
                {
                    previousLevel.NextLevel = orkLevel;
                }
                else
                {
                    firstLevel = orkLevel;
                }

                previousLevel = orkLevel;
            }

            roadmap.OrkLevel = firstLevel;
            return roadmap;
        }


        public async Task<List<DemandDto>> GetMostInDemandSkills()
        {
            var skills = new List<DemandDto>();

            var query = @"MATCH (:PositionFunction)-[:REQUIRES_SKILL]->(skill:Skill) 
                RETURN skill.Name AS Skill, COUNT(skill) AS Demand 
                ORDER BY Demand DESC 
                LIMIT 15";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query);

            await result.ForEachAsync(record =>
            {
                var demand = new DemandDto();
                demand.Name = record["Skill"].As<string>();
                demand.Demand = record["Demand"].As<int>();
                skills.Add(demand);
            });

            return skills;
        }
        public async Task<List<DemandDto>> GetMostInDemandKnowledges()
        {
            var knowledges = new List<DemandDto>();

            var query = @"MATCH (:PositionFunction)-[:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
                RETURN knowledge.Name AS Knowledge, COUNT(knowledge) AS Demand
                ORDER BY Demand DESC
                LIMIT 15";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query);

            await result.ForEachAsync(record =>
            {
                var demand = new DemandDto();
                demand.Name = record["Knowledge"].As<string>();
                demand.Demand = record["Demand"].As<int>();
                knowledges.Add(demand);
            });

            return knowledges;
        }

        public async Task<List<ProfessionOverlapDto>> GetOverlappingProfessions()
        {
            var query = @"// Step 1: Collect functions, skills, and knowledge for each PositionCard
                MATCH (card:PositionCard)-[:HAS_FUNCTION]->(func:PositionFunction)
                OPTIONAL MATCH (func)-[:REQUIRES_SKILL]->(skill:Skill)
                OPTIONAL MATCH (func)-[:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
                WITH card, COLLECT(DISTINCT func) AS functions, 
                     COLLECT(DISTINCT skill) AS skills, 
                     COLLECT(DISTINCT knowledge) AS knowledge

                // Step 2: Compare each PositionCard with every other PositionCard
                WITH card AS card1, functions AS functions1, skills AS skills1, knowledge AS knowledge1
                MATCH (card2:PositionCard)
                WHERE card1 <> card2

                // Step 3: Get functions, skills, and knowledge for the second card
                OPTIONAL MATCH (card2)-[:HAS_FUNCTION]->(func2:PositionFunction)
                OPTIONAL MATCH (func2)-[:REQUIRES_SKILL]->(skill2:Skill)
                OPTIONAL MATCH (func2)-[:REQUIRES_KNOWLEDGE]->(knowledge2:Knowledge)
                WITH card1, card2,
                     functions1, skills1, knowledge1,
                     COLLECT(DISTINCT func2) AS functions2,
                     COLLECT(DISTINCT skill2) AS skills2,
                     COLLECT(DISTINCT knowledge2) AS knowledge2

                // Step 4: Calculate overlaps manually without nested aggregates
                WITH card1, card2,
                     SIZE([f IN functions1 WHERE f IN functions2]) AS FunctionOverlapCount,
                     SIZE([s IN skills1 WHERE s IN skills2]) AS SkillOverlapCount,
                     SIZE([k IN knowledge1 WHERE k IN knowledge2]) AS KnowledgeOverlapCount
                where (FunctionOverlapCount + SkillOverlapCount + KnowledgeOverlapCount) <> 0
                // Step 5: Calculate total overlap and return results
                RETURN card1.Name AS Position1, card2.Name AS Position2,
                       FunctionOverlapCount, SkillOverlapCount, KnowledgeOverlapCount,
                       (FunctionOverlapCount + SkillOverlapCount + KnowledgeOverlapCount) AS TotalOverlap
                ORDER BY TotalOverlap DESC";

            var res = new List<ProfessionOverlapDto>();

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query);

            await result.ForEachAsync(record =>
            {
                var overlap = new ProfessionOverlapDto();
                overlap.Position1 = record["Position1"].As<string>();
                overlap.Position2 = record["Position2"].As<string>();
                overlap.FunctionOverlapCount= record["FunctionOverlapCount"].As<int>();
                overlap.SkillOverlapCount = record["SkillOverlapCount"].As<int>();
                overlap.KnowledgeOverlapCount = record["KnowledgeOverlapCount"].As<int>();
                overlap.TotalOverlap = record["TotalOverlap"].As<int>();
                res.Add(overlap);
            });
            return res;
        }

        public async Task<List<OverlapByOrkLevelsDto>> GetOverlappingSkillsByPositionCardName(string positionCard)
        {
            var skills = new List<OverlapByOrkLevelsDto>();

            var query = @"MATCH (card:PositionCard {Name: $positionCard})-[:HAS_FUNCTION]->(func:PositionFunction)
                MATCH (func)-[rel:REQUIRES_SKILL]->(skill:Skill)
                RETURN skill.Name AS Name, COUNT(DISTINCT rel) AS Count
                ORDER BY Count DESC";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query, new { positionCard });

            await result.ForEachAsync(record =>
            {
                var demand = new OverlapByOrkLevelsDto();
                demand.Name = record["Name"].As<string>();
                demand.OverlapCount = record["Count"].As<int>();
                skills.Add(demand);
            });

            return skills;
        }

        public async Task<List<OverlapByOrkLevelsDto>> GetOverlappingKnowledgesByPositionCardName(string positionCard)
        {
            var knowledges = new List<OverlapByOrkLevelsDto>();

            var query = @"MATCH (card:PositionCard {Name: $positionCard})-[:HAS_FUNCTION]->(func:PositionFunction)
                MATCH (func)-[rel:REQUIRES_SKILL]->(skill:Skill)
                RETURN skill.Name AS Name, COUNT(DISTINCT rel) AS Count
                ORDER BY Count DESC";

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query, new { positionCard });

            await result.ForEachAsync(record =>
            {
                var demand = new OverlapByOrkLevelsDto();
                demand.Name = record["Name"].As<string>();
                demand.OverlapCount = record["Count"].As<int>();
                knowledges.Add(demand);
            });

            return knowledges;
        }

        public async Task<List<RemainingDataDto>> GetRemainingSkillsKnowledges(string positionCard, string[] knownSkills, string[] knownKnowledges)
        {
            var query = @"WITH $knownSkills AS knownSkills, 
                     $knownKnowledges AS knownKnowledges, 
                     $positionCard AS targetName
                MATCH (pc:PositionCard {Name: targetName})-[:HAS_FUNCTION]->(pf:PositionFunction)
                OPTIONAL MATCH (pf)-[rs:REQUIRES_SKILL]->(skill:Skill)
                OPTIONAL MATCH (pf)-[rk:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
                WITH pf, rs, rk, skill, knowledge, knownSkills, knownKnowledges
                WHERE (skill IS NULL OR NOT skill.Name IN knownSkills)
                   OR (knowledge IS NULL OR NOT knowledge.Name IN knownKnowledges)
                WITH 
                  COALESCE(rs.OrkLevel, rk.OrkLevel) AS orkLevel,
                  CASE 
                    WHEN skill IS NOT NULL AND NOT skill.Name IN knownSkills 
                    THEN skill.Name ELSE NULL 
                  END AS missingSkill,
                  CASE 
                    WHEN knowledge IS NOT NULL AND NOT knowledge.Name IN knownKnowledges 
                    THEN knowledge.Name ELSE NULL 
                  END AS missingKnowledge
                WITH orkLevel,
                     COLLECT(DISTINCT missingSkill) AS allMissingSkills,
                     COLLECT(DISTINCT missingKnowledge) AS allMissingKnowledges
                RETURN 
                  orkLevel,
                  [s IN allMissingSkills WHERE s IS NOT NULL] AS missingSkills,
                  [k IN allMissingKnowledges WHERE k IS NOT NULL] AS missingKnowledges
                ORDER BY orkLevel";
            
            var res = new List<RemainingDataDto>();

            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(query, new { knownSkills, knownKnowledges, positionCard});

            await result.ForEachAsync(record =>
            {
                var remaining = new RemainingDataDto();
                remaining.OrkLevel = record["orkLevel"].As<int>();
                remaining.RemainingKnowledges = record["missingKnowledges"].As<List<string>>();
                remaining.RemainingSkills = record["missingSkills"].As<List<string>>();
                res.Add(remaining);
            });

            return res;

        }

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}