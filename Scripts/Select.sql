--select all nodes related to specific standard:
MATCH (ps:PositionStandart {Name: "Наименование ПС:«Бизнес-анализ в информационно- коммуникационных технологиях»."})
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