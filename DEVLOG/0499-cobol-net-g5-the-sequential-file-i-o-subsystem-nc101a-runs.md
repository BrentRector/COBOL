## Entry 499 — COBOL.NET G5: the sequential file I/O subsystem (NC101A runs end-to-end)

The G5 body: sequential files (ISO §14.9; COBOLNET_DESIGN §8) — the design's "sequential → relative → indexed"
staging, first stage. The control logic the legacy `SequentialFileHandler`/`FileRuntime` proved over 364 NIST tests,
re-substrated from a byte buffer to the record's **character image** (a C# `string`) — a COBOL record is a typed
value; the only edge where it becomes characters is the on-disk codec. No byte `ProgramState`.

**Runtime (`Cobol.Net.Runtime/IO/`).** `SequentialFile` — the connector: OPEN (INPUT/OUTPUT/EXTEND/I-O, OPTIONAL),
CLOSE (+ the WRITE…ADVANCING trailing newline), WRITE (plain + print-control ADVANCING n LINES / PAGE), READ NEXT,
REWRITE, with the ISO §9.1.13 status codes and the §14.9.30/§14.9.35 read-position state machine ported verbatim. Two
on-disk shapes — line-sequential (newline-framed) and record-sequential (fixed-width blocks + the raw ADVANCING
stream). `CobolFile` — the static facade the backend emits calls to (one registry of named connectors + the verbs +
FILE STATUS / AT END accessors); `ResolveHostPath` matches the legacy convention (`<lowercased>.txt`) so the
differential corpus finds the same file.

**Data model (`DataBinder`).** Bind the FILE-CONTROL SELECT clauses (assign / organization / access / OPTIONAL /
FILE STATUS) → `FileModel`, and the FILE SECTION FD records into the storage forest (they emit as Program fields, like
WORKING-STORAGE). The **shared record area** (ISO §9.1.2 — multiple `01`s under one FD occupy ONE area) is modeled by
synthesizing each secondary record as a REDEFINES of the first, so the existing `RedefinesClass` tier machinery makes
them alias one backing (the singular-pattern rule — no second sharing mechanism; advisor-guided). The WS entry loop is
factored into a reusable `BindEntries` shared by WORKING-STORAGE and each FD; the latent root-name-uniquing no-op is
fixed (roots now actually recorded in the shared C#-name scope, needed for FD + WS coexistence).

**Bound tree + binder + emitter.** `BoundOpen/Close/Write/Read/Rewrite` (carrying the `FileModel`, the resolved record
`Place`, the FROM operand, the ADVANCING phrase, and the AT END imperatives); `StatementBinder` resolves file-names →
`FileModel` and a WRITE/REWRITE record-name back to its owning file; a non-sequential organization carries a loud
reason. The emitter registers every file at `Main` start and `CobolFile.CloseAll()` in a `finally` (ISO §14.6 implicit
close), then emits a `CobolFile` call per verb (WRITE…FROM is a MOVE-then-WRITE; READ distributes the image into the
record area and, with INTO, MOVEs it on; FILE STATUS stored after each verb). The file-IO `using` + the registration
are gated on the program having files, so a file-less program's emitted C# is byte-identical to before.

**Verification.** NC101A — the first full NC program — now **compiles and runs end-to-end**; the print file (OPEN
OUTPUT / WRITE…AFTER ADVANCING / CLOSE) is produced and its report structure (headers, PASS lines, footer) matches the
golden. 7 `FileIoDifferentialTests` pin the data-file verbs to the legacy oracle (round-trip WRITE→READ, WRITE FROM /
READ INTO, EXTEND append, FILE STATUS 00/10, OPTIONAL-absent AT END, multi-01 shared area, line-sequential ADVANCING
read-back). Conformance 213 → 220; 13 unit; green. Greenfield-only — the shared front-end and the legacy oracle are
untouched. **Remaining for NC101A all-green:** 11 numeric `FAIL*` (MULTIPLY/DIVIDE edge cases — e.g. an overpunch-sign
image `0000R`, a multiply yielding 0) — pre-existing arithmetic gaps the corpus now surfaces, NOT file I/O; the next
slices. (Per the advisor: land the subsystem + targeted tests, then iterate NC101A's unrelated gaps.)

