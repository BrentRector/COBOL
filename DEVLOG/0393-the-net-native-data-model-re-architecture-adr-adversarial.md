## Entry 393 — The .NET-native data-model re-architecture: ADR + adversarial review (design only; the migration is the next session's #1 priority)

The owner directed a foundational redesign toward "the best native .NET implementation of COBOL." An extended
design dialogue this session settled the data model, captured in **`docs/DATA_MODEL_ARCHITECTURE.md`** (the ADR)
and stress-tested in **`docs/DATA_MODEL_REVIEW.md`** (a ~57-agent adversarial review workflow: 7 dimensions ×
find→refute→synthesize; 48 findings, 45 verified; verdict **proceed-with-changes**).

The converged design (supersedes the prior byte-array-is-the-substrate model):
- **Typed-native is the default.** COBOL records → .NET `record struct`; elementary items → native value types
  (`long`/`decimal`/`double`/`bool`); **character data (PIC X *and* PIC N) → `string` (UTF-16)** — whole-field
  MOVE is a free reference copy, the verbs map to BCL + `ReadOnlySpan<char>` (ordinal), zero conversion at .NET
  boundaries.
- **In-memory representation is decoupled from external encoding** — single-byte/EBCDIC/UTF-8 is a `CODE-SET`
  boundary concern, never an in-memory layout.
- **A byte image is a bounded, classifier-scoped fallback** for REDEFINES/RENAMES type-puns, file records, and a
  few measured hot loops — an inline value type embedded in the typed record (a value-type union or buffer+views),
  never a heap `byte[]` and never the substrate. COMP-3 stays typed (`decimal`); a same-layout group MOVE is a
  value-type struct assignment in *either* representation.
- **Pointers → managed references; OO → .NET classes.** Cecil/CIL stays primary through the migration; a Roslyn C#
  backend with Cecil as a byte-exact differential oracle is a later phase.

Owner-caught corrections during the dialogue (each made the design better): PIC X need not be an inline buffer —
`string` is the right default (immutability makes whole-MOVE a free reference copy); byte-exactness is an
I/O/overlay *boundary* concern, not an in-memory one; COMP-3 byte-backing is NOT "less native" (it is emulated
either way) — the real axis is per-op vs boundary pack/unpack, and a same-layout group MOVE is a struct assignment
regardless.

Review high findings (all folded into the ADR): CALL…BY REFERENCE / LINKAGE need *unconditional* byte triggers
(separate compilation makes the callee's view unknowable); refmod of a numeric DISPLAY item must trigger
byte-backing; group ops with COMP members and class conditions over a group need byte *materialization* (not
permanent byte-backing — the over-conservative original was corrected); PROGRAM COLLATING SEQUENCE must not be
dropped by an ordinal compare. Owner decisions recorded for the migration: numeric substrate = `BigInteger` (not
`decimal`) for 19–31 digits (resolve before Stage 1), and the VALUE/INITIALIZE-of-typed-fields initialization
model.

No production code changed here (design only) beyond the two companion bug fixes (Entry 392). **The migration is
the next session's first priority** — see the banner in `docs/ISO2023_CONFORMANCE_PLAN.md` §0.5 and
`resume-prompt.md`.

