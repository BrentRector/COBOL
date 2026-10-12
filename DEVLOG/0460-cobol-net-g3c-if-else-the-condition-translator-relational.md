## Entry 460 — COBOL.NET G3c: IF / ELSE + the condition translator (relational + AND/OR/NOT, numeric & string)

`IF … THEN … ELSE … END-IF` → C# `if/else` (THEN/ELSE blocks split at the ELSE token). New `RenderCondition`
recursively translates the COBOL condition grammar: relational comparisons combined with `AND`/`OR`/`XOR`/`NOT` and
parentheses. A comparison picks numeric vs alphanumeric form by operand type — numeric → `(decimal)a <op> (decimal)b`
(reusing `RenderArith`), alphanumeric → `CobolString.Compare(a,b) <op> 0` (COBOL space-extended ordering). All COBOL
operator spellings (symbolic + word forms + `NOT …`) map via `MapOperator` (a self-review caught + fixed a
double-negation bug: word-form `NOT EQUAL` had collided with the `<>` branch → wrongly `==`). Class/sign/condition-
name + abbreviated relational forms are conservative `false` fallbacks for now. Verified: `A<B`, `A>B`, `A=10 AND
B=20`, `NM="BOB"` (space-extended), `A NOT EQUAL B` all correct.

