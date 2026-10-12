## Entry 1203 — 2026-08-07 12:55 PDT — R02: the underscore, the "nor last" half of the same sentence — and a BOM I wrote broke CI while every local gate stayed green

**§8.3.2.1 IS ONE SENTENCE SAYING TWO THINGS, AND BOTH WERE WRONG.** "Each character of a COBOL word … shall be
selected from the set of basic letters, basic digits, extended letters, and the basic special characters
**hyphen and underscore**. The hyphen or underscore **shall not appear as the first or last character** in such
words."

**HALF ONE — the underscore was not a word character at all.** `01 MY_NAME PIC X(6).` is legal at three of the
four editions this compiler targets and parsed at NONE of them, dying as
`error COBOL0001: no viable alternative at input 'MY_'` — an unexplained rejection, not a named edition
diagnostic.

**HALF TWO — "nor last" was unenforced for the HYPHEN, and predates this item.** The ALPHA-start `NAME_BODY`
alternative ended `[a-z0-9]`, so `AB-` was refused; the two DIGIT-start alternatives ended in `*` over the
separator class, so **`1A-` and `42-X-` were ACCEPTED**. Adding the underscore to that class without looking
would have inherited the hole for the new character. Factoring the tail into `NAME_TAIL` — which cannot end on a
separator — closes it for BOTH characters across all three alternatives at once.

**THE GATE IS BIND-TIME, AND THAT IS FORCED.** The lexer admits the underscore at EVERY edition
(superset-parse / bind-narrow) and COBOL-85 rejects through the new construct `user-word-underscore-2002` →
COBOLNET0900, raised in `VisitCobolWord` beside the §8.3.2.1 word-LENGTH check — same clause, same visitor. A
lexer that refused it below 2002 would emit `no viable alternative` again: the very symptom being fixed. **The
token must lex so the gate can name the edition.**

⭐ **THE EXTERNAL ORACLE CORROBORATED BOTH FIXES, WITH EIGHT PER-CASE FLIPS, ALL TOWARD AGREEMENT.** Six are
R02's: real GnuCOBOL programs whose PROGRAM-IDs carry underscores (`Move_Basic_P_Pic`, `MoveDeEditing_2..5`,
`prog_a`) that we REJECTED and now accept. Two are R03's, pending since it landed because that item took only a
filtered conformance gate: `syn_literals:1105` (`X"1"`) and `:1178` (`NX"1"`) — and `syn_literals` is
GnuCOBOL's own SYNTAX test, which EXPECTS those rejected, so it independently confirms the §8.3.3.2.3 r6
reading COBOLNET1635 encodes. §0's mechanical rule classes every divergence→AGREE flip a FIX: eight fixes, zero
regressions. Baseline regenerated, exactly 8 rows changed, fresh run 0 flips.

⛔ **AND THE REAL LESSON OF THIS ENTRY IS A BOM I WROTE THAT BROKE CI WHILE EVERY LOCAL GATE STAYED GREEN.**
The R03 commit (`5797d897`) shipped `CobolLexer.g4` with a UTF-8 byte-order mark, because a tool edit used
Python's `utf-8-sig` — an encoding that STRIPS a BOM on read and **ADDS one on write**, so a read/modify/write
round trip introduces one into a file that never had it. ANTLR answered
`error(50): CobolLexer.g4:1:0: syntax error: '∩' came as a complete surprise to me` and the Windows Release job
failed on `main`.

**THREE FAILURES COMPOUNDED, AND THE THIRD IS THE ONE THAT MATTERS:**
1. I stopped watching CI after the PB50/PB54 run and kept committing — six commits landed unwatched.
2. Every LOCAL gate stayed green, by construction: the incremental build had already produced the generated
   parser, so the grammar compiler never re-ran on the changed file the way a clean checkout does. Conformance
   2686/2686 and Unit 4029/4029 both passed on a tree that could not build from scratch.
3. ⛔ **ENTRY 1198 — MINE, YESTERDAY — DESCRIBED EXACTLY THIS HAZARD AND I WALKED INTO IT.** R03's own run was
   `cancelled` when R01's push superseded it (`cancel-in-progress`), so the first run to complete the Windows job
   was R01's, one commit later. Writing a hazard down is not the same as acting on it.

**THE GUARD IS BYTE-LEVEL BECAUSE THE FAILURE IS INVISIBLE TO EVERY OTHER INSTRUMENT** — the file reads
correctly in any editor, the diff shows nothing, and only a cold build objects. `SourceEncodingDriftTests`
checks the first three bytes of every committed `.cs` and `.g4`, and it **immediately found FOUR more BOM'd
files my manual sweep had missed**: the three fragment parsers from the PB50/PB54 rewiring, plus
`ReferenceResolver.cs`, whose BOM PREDATES this session (normalized here rather than exempted, and called out
so it does not read as a silent drive-by). A hand list would have fixed one file and left four.

**GATE.** `=== BATTERY: ALL GREEN ===` on ONE whole run — Conformance **4244/4244** zero skipped · Unit
**4033/4033** · characterization **33/33** · guard-fast ALL GREEN with NIST **353 MATCH / 0 REGRESSION** and
NIST AUDIT CLEAN · GnuCOBOL differential 1323 cases, **0 PER-CASE FLIPS** against the regenerated baseline. The
FULL battery rather than a filter because `NAME_BODY` is how every COBOL word in every program is lexed, in both
lexer modes — and the "nor last" tightening REMOVES acceptances, so the differential is the load-bearing
evidence, not the conformance count. Golden `r02_underscore_in_cobol_words` (9 assertions) · negative
`r02-word-trailing-separator`.

