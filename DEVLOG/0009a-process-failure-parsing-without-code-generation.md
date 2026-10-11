## Entry 009 — 2026-03-13: Process Failure — Parsing Without Code Generation

### The Mistake

During the lexer/parser rewrite (Entry 008), I added 6 new statement parsers (EVALUATE,
MULTIPLY, DIVIDE, SET, SEARCH, GOBACK) but shipped them with NOP placeholders in the CIL
emitter instead of real code generation. This meant:

- `MULTIPLY 6 BY X` parsed correctly into a MultiplyStatement AST node
- The CIL emitter saw MultiplyStatement and emitted `nop` — doing nothing
- The program compiled and ran without errors
- **X was unchanged.** The multiplication silently didn't happen.

This is the worst kind of bug: it produces wrong results without any error message. A
compilation failure would have been far better than silent data corruption.

### Why It Happened

I treated parsing and code generation as separate phases of work instead of building them
together. The plan was organized as "Phase 1: Lexer, Phase 2: Parser Infrastructure,
Phase 4: Statement Parsers" — code generation was an afterthought, not part of each
statement's definition of done.

The unit tests I wrote only verified parsing (correct AST structure). The integration tests
I had only covered pre-existing statements. I added 9 new parser tests and 0 new integration
tests for the new statements — testing that the parser produced the right tree shape, but
never checking that the compiled program produced the right output.

### The Fix

The user caught this and correctly called it a failure. I then:
1. Added runtime methods: MultiplyBy, DivideInto, DivideGiving in CobolProgram.cs
2. Implemented real CIL emission for MULTIPLY, DIVIDE, SET, and rewrote EVALUATE
   (which was also skeletal/broken)
3. Fixed PERFORM VARYING and PERFORM UNTIL (out-of-line) code generation
4. Added 5 end-to-end integration tests that verify **correct output values**:
   - MULTIPLY 6 BY 7 → "00042"
   - DIVIDE 42 BY 7 → "00006"
   - EVALUATE 2 → selects "Two" branch
   - GOBACK → stops execution
   - SET TO 42 → "042"

### Lesson Learned

**Every new statement must ship as a complete vertical slice: AST node + parser + runtime
method + CIL emitter + output-verifying integration test.** No parser-only commits.
Parsing without emission is worse than not parsing at all, because it creates programs
that compile but produce silently wrong results.

This is now recorded as a permanent feedback rule for future sessions.

### Also Fixed in This Round
- Scope terminator handling for CALL (END-CALL), WRITE (END-WRITE), STRING (END-STRING),
  UNSTRING (END-UNSTRING), REWRITE (END-REWRITE), DELETE (END-DELETE), START (END-START)
- Added generalized SkipExceptionPhrases() for ON EXCEPTION/OVERFLOW/INVALID KEY/AT END
- Abbreviated combined relations: `A > B AND C` → `A > B AND A > C`
- Fixed UNSTRING TALLYING IN (IN is now InKeyword, not Identifier)

---

