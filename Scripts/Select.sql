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