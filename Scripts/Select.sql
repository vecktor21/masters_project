--select all nodes related to specific standard:
MATCH (ps:PositionStandart {Name: "Наименование ПС:«Системный анализ в информационно- коммуникационных технологиях»."})
OPTIONAL MATCH (ps)-[:HAS_CARD]->(card:PositionCard)
OPTIONAL MATCH (card)-[:HAS_FUNCTION]->(function:PositionFunction)
OPTIONAL MATCH (function)-[:REQUIRES_SKILL]->(skill:Skill)
OPTIONAL MATCH (function)-[:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
OPTIONAL MATCH (card)-[:HAS_ORK_LEVEL]->(orkLevel:OrkLevel)
RETURN ps, card, function, skill, knowledge, orkLevel

--select all nodes related to specific standard with function level:
MATCH (ps:PositionStandart {Name: "Наименование ПС:«Бизнес-анализ в информационно- коммуникационных технологиях»."})
OPTIONAL MATCH (ps)-[:HAS_CARD]->(card:PositionCard)
OPTIONAL MATCH (card)-[:HAS_FUNCTION{OrkLevel:4}]->(function:PositionFunction)
OPTIONAL MATCH (function)-[:REQUIRES_SKILL]->(skill:Skill)
OPTIONAL MATCH (function)-[:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
OPTIONAL MATCH (card)-[:HAS_ORK_LEVEL]->(orkLevel:OrkLevel)
RETURN ps, card, function, skill, knowledge, orkLevel



--select all levels
MATCH (l:OrkLevel) return l

--unique functions
match(n:PositionFunction) return distinct n.FunctionName order by 1 asc

--проверить на дубли функций
call{match (c:PositionCard)-[:HAS_FUNCTION]->(f:PositionFunction) 
return distinct ID(c) as c_id,  c.Name as c_name, f.FunctionName as f_name order by 1 asc}
return f_name, count(*)

--вывести все навыки по функциям
match(c:PositionCard)-[:HAS_FUNCTION]->(p:PositionFunction)
match(p)-[:REQUIRES_KNOWLEDGE]->(k:Knowledge)
with c, p, collect(k.Name) as ks
RETURN c.Name, p.FunctionName, CASE WHEN size(ks) > 1 
THEN ks[0] + reduce(x = '', z IN tail(ks) | x + '\r\n' + z)
ELSE ks[0]
END as knowledges


--проверка на дубли навыков
call{match (c:PositionCard)-[:HAS_FUNCTION]->(f:PositionFunction)
match (f)-[:REQUIRES_KNOWLEDGE]->(k:Knowledge) 
return distinct ID(c) as c_id,  c.Name as c_name, f.FunctionName as f_name, k.Name as k_name order by 1 asc}
return
//в разрезе должности
//c_name, 
//в разрезе функции
f_name,k_name, count(*)


--просмотр навыков по уровням
match(p:PositionCard)-[hf:HAS_FUNCTION]->(f:PositionFunction),
(f)-[rk:REQUIRES_SKILL]->(k:Skill)
return p.Name, f.FunctionName, collect(distinct hf.OrkLevel) as function_levels, collect(distinct rk.OrkLevel) as skill_levels, k.Name


--просмотр знаний по уровням
match(p:PositionCard)-[hf:HAS_FUNCTION]->(f:PositionFunction),
(f)-[rk:REQUIRES_KNOWLEDGE]->(k:Knowledge)
return p.Name, f.FunctionName, collect(distinct hf.OrkLevel) as function_levels, collect(distinct rk.OrkLevel) as knowledge_levels, k.Name


--скилы которые дублируются по уровням
MATCH (card:PositionCard {Name: "«Администратор баз данных»"})
MATCH (card)-[:HAS_FUNCTION {OrkLevel: 5}]->(funcLow:PositionFunction)-[:REQUIRES_SKILL {OrkLevel: 5}]->(skill:Skill)
MATCH (card)-[:HAS_FUNCTION {OrkLevel: 6}]->(funcHigh:PositionFunction)-[:REQUIRES_SKILL {OrkLevel: 6}]->(skill)
RETURN skill.Name AS CommonSkill, COUNT(skill) AS OverlapCount

--знания которые дублируются по уровням
MATCH (card:PositionCard {Name: "«Администратор баз данных»"})-[:HAS_FUNCTION]->(func:PositionFunction)
MATCH (func)-[rel:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
RETURN knowledge.Name AS Knowledge, COUNT(DISTINCT rel) AS KnowledgeRelationCount
ORDER BY KnowledgeRelationCount DESC


--найти все пересекающиеся профессии
// Step 1: Collect functions, skills, and knowledge for each PositionCard
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
ORDER BY TotalOverlap DESC


--востребованные навыки
MATCH (:PositionFunction)-[:REQUIRES_SKILL]->(skill:Skill)
RETURN skill.Name AS Skill, COUNT(skill) AS Demand
ORDER BY Demand DESC
LIMIT 10

--востребованные знания
MATCH (:PositionFunction)-[:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
RETURN knowledge.Name AS Knowledge, COUNT(knowledge) AS Demand
ORDER BY Demand DESC
LIMIT 10


--получить все должности
match(c:PositionCard) return distinct c.Name


--дублирующиеся навыки по уровням
MATCH (card:PositionCard {Name: "«Администратор баз данных»"})-[:HAS_FUNCTION]->(func:PositionFunction)
MATCH (func)-[rel:REQUIRES_SKILL]->(skill:Skill)
RETURN skill.Name AS Skill, COUNT(DISTINCT rel) AS SkillRelationCount
ORDER BY SkillRelationCount DESC

--дублирующиеся знания по уровням
MATCH (card:PositionCard {Name: "«Администратор баз данных»"})-[:HAS_FUNCTION]->(func:PositionFunction)
MATCH (func)-[rel:REQUIRES_KNOWLEDGE]->(knowledge:Knowledge)
RETURN knowledge.Name AS Knowledge, COUNT(DISTINCT rel) AS KnowledgeRelationCount
ORDER BY KnowledgeRelationCount DESC