## Entry 335 — WS-SPEC fix: variadic string functions accept SPACE-separated literal args

`SplitSubscriptTokens` (the subscript / function-argument splitter) did not treat a space before a string or
unsigned-decimal literal as an argument boundary, so `FUNCTION CONCATENATE("ab" "cd" "ef")` passed only `"ab"`.
A string / decimal literal is never an arithmetic operand, so a space before it always starts a new argument:
added `SUB_STRINGLIT` + `SUB_DECIMALLIT` to the split-trigger set (the existing endsWithOperator/Qualifier/Function
guards still prevent splitting mid-expression, e.g. `9 * A`). CLI-verified → `abcdef`. Test:
`SpecFixTests.Concatenate_SpaceSeparatedLiteralArgs_PassesAll`. 5th backlog fix re-implemented on main.

