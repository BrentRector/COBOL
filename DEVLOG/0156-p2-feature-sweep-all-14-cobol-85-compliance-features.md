## Entry 156 — 2026-03-27: P2 Feature Sweep — All 14 COBOL-85 Compliance Features

**Context:** After P0+P1 bug fixes (34 bugs), the spec compliance audit identified 14 COBOL-85
required features that were missing. Dispatched 6 parallel agents — 5 direct, 1 in a git worktree
(for SORT/MERGE which touches nearly every file).

**Features implemented (6 agents, 14 items):**

Agent 1 (worktree): SORT/MERGE/RELEASE + SD file descriptions
- SD grammar rule, SORT/MERGE with all clauses (KEY, DUPLICATES, COLLATING, USING/GIVING,
  INPUT/OUTPUT PROCEDURE THRU)
- BoundSortStatement/BoundMergeStatement/BoundReleaseStatement
- 6 new IR instructions, full 3-phase lowering (input→sort→output)
- SortRuntime.cs: in-memory sort engine (limitation documented; external merge sort needed
  for production). Windows provides no OS-level record-oriented sort API.
- RETURN rewritten from stub to full implementation
- 3 integration tests

Agent 2: CobolCategory.Alphabetic + 10 validation checks
- New Alphabetic category with full MOVE compatibility matrix (ISO Table 16)
- PIC A items now correctly classified (was Alphanumeric)
- 8 new diagnostic descriptors, 14 new unit tests in MoveValidationTests.cs
- Validations: MOVE ZERO→Alphabetic, HIGH-VALUE→Numeric, noninteger→Alphanumeric,
  BLANK+JUSTIFIED, OCCURS on 66, VALUE on REDEFINES, VALUE on OCCURS subordinate,
  SEARCH ALL WHEN must be equality, CORRESPONDING excludes RENAMES, sign condition
  on non-numeric

Agent 3: SPECIAL-NAMES features (CLASS + SYMBOLIC CHARACTERS + ALPHABET)
- User-defined CLASS: full pipeline SemanticBuilder→BoundUserClassConditionExpression→
  IrUserClassCondition→CIL→IsInUserClass runtime. ClassDefinition + AlphabetDefinition types.
- SYMBOLIC CHARACTERS: resolved as literal expressions in BoundTreeBuilder
- ALPHABET/collating sequence: new IR instructions + CompareAlphanumericWithSequence
  with 256-byte mapping. PROGRAM COLLATING SEQUENCE wired through.
- 4 integration tests

Agent 4: EXIT PERFORM CYCLE + OCCURS DEPENDING ON runtime
- CYCLE lexer token + IsCycle flag + _performContinueStack for continue-to-increment
- ODO: SEARCH/SEARCH ALL now use runtime DEPENDING ON field value, not static MaxOccurs
- 3 integration tests

Agent 5: File I/O (5 features)
- Open-mode enforcement: status 47/48/49 in all 3 file handlers
- LINAGE clause + END-OF-PAGE: grammar, SemanticBuilder, runtime line counter
- RELATIVE KEY IS: grammar, FileSymbol, random READ by key field
- SELECT OPTIONAL: status "05" for missing optional files
- USE declaratives: parsed, bound, registered (execution deferred)
- 8 integration tests

Agent 6: EXTERNAL/GLOBAL on data items
- Grammar clauses, DataSymbol flags, 5 validation diagnostics (CBL3115-3119)
- Runtime shared storage deferred with warnings
- 12 tests (6 unit + 6 integration)

**Worktree merge:** SORT/MERGE agent ran in isolated worktree (branch worktree-agent-a0cfb422).
Merged via `git apply --3way` — 2 conflicts resolved manually (BoundNodes enum + SemanticBuilder
visitor). ANTLR regenerated after merge. Clean build + all tests pass.

**Results:** 256 unit + 224 integration + 60 NIST guard = ALL GREEN.
Total session: 34 bugs fixed (P0+P1) + 14 features implemented (P2) = 48 items from audit.

---

