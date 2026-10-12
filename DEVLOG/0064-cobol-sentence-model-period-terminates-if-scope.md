## Entry 064 — 2026-03-15: COBOL Sentence Model — Period Terminates IF Scope

### The Problem

F1-1 showed DE-LETE instead of PASS despite correct arithmetic and comparison.
IL dump revealed the join block after the IF contained only the fall-through return
(`ldc.i4 27` = MPY-DELETE-F1-1), not the GO TO (`ldc.i4 29` = MPY-WRITE-F1-1).

Root cause: The grammar's IF rule had `(ELSE imperativeStatement*)?` where
`imperativeStatement: statement+` greedily consumed ALL statements until the
method end. The period after `GO TO MPY-FAIL-F1-1.` was consumed by the GO TO
statement's own `DOT?`, so the ELSE continued to eat `GO TO MPY-WRITE-F1-1`
as a second statement inside the ELSE branch. The GO TO after the IF was never
a separate paragraph-level statement.

This is the classic COBOL "period ends IF" problem.

### The Fix: Sentence Model

Introduced `sentence` as the only rule that owns DOT in the procedure division:

```antlr
sentence
    : statement+ DOT
    ;

paragraphDeclaration
    : paragraphName DOT sentence*
    ;
```

Removed `DOT?` from ALL procedure-division statement rules (40+ rules). Statements
no longer consume periods. The period belongs to the sentence, which naturally
terminates the IF scope.

Updated BoundTreeBuilder and SemanticBuilder to iterate `sentence → statement`
instead of raw `statement*`.

### Result

**NC101A: 48 of 89 tests pass** (was 21 of 60). Massive improvement:
- F1-1 flipped from DE-LETE to **PASS**
- Test count jumped from 60 to 89 (sentence model allows more statements to parse)
- Footer now complete: FAILED/DELETED/INSPECTION counts + copyright line
- 35/35 category tests still pass

### Remaining Failures (41 of 89)

- F1-2: FAIL (72 vs 73) — ROUNDED not implemented
- F1-3/F1-4: FAIL — ON SIZE ERROR not implemented
- F1-6, F1-7, F1-9, F1-11, F1-12: DE-LETE — ROUNDED in MULTIPLY grammar
- F1-13+: ON SIZE ERROR sub-tests not executing

---

---

