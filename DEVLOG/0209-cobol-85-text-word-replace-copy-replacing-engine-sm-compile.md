## Entry 209 — 2026-05-29: COBOL-85 text-word REPLACE/COPY REPLACING engine (SM COMPILE_FAIL → 0)

Replaced the naive `string.Replace`-based COPY REPLACING / REPLACE with a proper COBOL-85
text-word matcher (ISO §7.4 REPLACE, §7.5 COPY REPLACING) — implemented to the specification,
not to the tests.

**Text-word tokenizer** (`TokenizeTextWords`, ISO §7.3.2): white space separates words and is
otherwise insignificant; `(` and `)` are standalone words; an alphanumeric literal (with its
quotes) is one word; a period/comma/semicolon that is not a decimal point is a separator word.
Each word keeps its source span.

**Matcher** (`ApplyReplacements`): each operand is a text-word sequence; matching slides over the
source words comparing word-for-word, case-insensitively, ignoring intervening white space and
line breaks. At each position the first operand (in source order) that matches wins; the matched
source span is replaced verbatim by the replacement text and is not rescanned. This is what makes
multi-line pseudo-text (`==PERFORM FAIL. ==`, operands split across continuation lines) match the
single-spaced library text, and what stops `"Z"`→… from corrupting unrelated words.

**Operand reader** (`ReadReplaceOperand`, COBOL-85 COPY … REPLACING operand forms): pseudo-text
`==…==`, an alphanumeric literal (quotes preserved), or an **identifier** — a data-name with an
`OF`/`IN` qualifier chain and an optional subscript (`WRK IN GRP-002 (1)`). The previous reader
took only a single word, so a qualified/subscripted operand-2 was mis-parsed into bogus `IN→…`
pairs that corrupted the whole program. A new `ReadTextWord` reads one text word (handles signed
words like `+2`/`-3`, which `ReadWord` could not). Both COPY REPLACING and the REPLACE statement
now route through the same matcher.

Result: **SM COMPILE_FAIL 2→0; CLEAN 8→9.** Every SM program now compiles and runs. Guard ALL
GREEN (1000 / 336 / 137). Remaining SM is value-correctness (6 FAIL*) + 2 flagging modules.

