using Diplom.Constants;
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
                            MERGE (f)-[:REQUIRES_SKILL]->(s) return s";

                            var skillRes = await tx.RunAsync(createSkillQuery, new
                            {
                                skill,
                                functionName = function.FunctionName
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
                            MERGE (f)-[:REQUIRES_KNOWLEDGE]->(k) return k";

                            var knowledgeRes = await tx.RunAsync(createKnowledgeQuery, new
                            {
                                knowledge,
                                functionName = function.FunctionName
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

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}