## Entry 1226 â 2026-08-08 13:08 PDT â R17: the float literal gets its signed twins â and the drift guard got audited against the hole it was built for

`FUNCTION EXP(-1.5E3)` drew "takes 1 argument(s); 2 given" because the float shape was the one
signed-capable literal body with no twins: the signed-DECIMAL rule won maximal munch at "-1.5" and orphaned
"E3" as an identifier. The fix is three lexer rules sharing one factored FLOAT_BODY, all re-typed into the
existing FLOATLIT vocabulary â the sign rides the token text, `numericLiteralCore` already accepted the
type, and the emitter's E-form arm renders the signed text as a C# double literal unchanged.

Two things the probes caught mid-flight:

- SUBSCRIPT mode had NO float shape at all, so fixing the DEFAULT-mode twin MOVED the keyword-omitted
  spelling's failure rather than removing it â `EXP(+1.5E1)` went from a silent two-argument mis-lex to a
  loud 'no viable alternative' at the OUTER capture, because `subToken` did not list FLOATLIT. Both SUB
  forms + the capture entry landed together; e^15 now computes through the re-parse.
- My drift guard's first draft would NEVER have caught the original hole: it scrapes `fragment *_BODY`
  declarations, and the pre-R17 FLOATLIT carried its body INLINE â no fragment, no assertion. The guard
  gained a third assertion forcing the three DEFAULT-mode numeric tokens to be bare fragment references, so
  a future literal shape cannot exist outside the twin assertions. A guard that would have been green
  through the defect it commemorates is not a guard (feedback_green_gates_arent_evidence).

Probes: EXP(-1.5E1) = e^-15 exact Â· keyword-omitted EXP(+1.5E1) = e^15 Â· MAX(-1.5E3, 2) = 2. Golden
signed_float_literal_argument. Gate: Corpus|Intrinsic|VersionMatrix 2665/2665 (the full edition matrix
re-compiled with the new lexer) Â· characterization 33/33 Â· SignedLiteralShapeDriftTests 3/3.

