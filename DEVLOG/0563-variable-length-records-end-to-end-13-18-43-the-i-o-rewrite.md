## Entry 563 — 2026-06-10 20:37 PDT — Variable-length records end-to-end (§13.18.43) + the I-O REWRITE stack → SQ212A/SQ228A/RL206A/RL211A/IX106A all green (806 conformance)

**Three stacked findings, one coherent feature.** It started as the brief's one-line C7 ("OPEN I-O opens a
read-only stream") and unwound into the complete §13.18.43 variable-length record subsystem.

**1. The I-O REWRITE stack (IX106A).** `SequentialFile.Open` I-O built a read-only `StreamReader` — `Rewrite`'s
`CanWrite` guard always failed silently to '30'. Opening `FileAccess.ReadWrite` exposed bug #2: the rewrite
block-start came from `BaseStream.Position` — but a `StreamReader` BUFFERS, so the base position is the
buffer-fill boundary, never the read position; the rewrite corrupted neighboring bytes. The block start is now
the LOGICAL offset (characters consumed; Latin1 is 1:1 chars↔bytes), with a `FillChars` exact-fill loop that
also fixes the latent partial-`Read` truncation in the fixed path. IX106A green.

**2. The fix EXPOSED SQ228A as an accidental pass:** its USE-declarative test expects the REWRITE of a
DIFFERENT-SIZE record (RECSIZE 130 over a 120 record) to fail — previously every REWRITE failed '30', firing
the declarative for the wrong reason. The spec-correct status is **'44'** (§14.9.35 GR16: a record-sequential
REWRITE whose size differs from the record being replaced; §9.1.13.6 item 2b). That is the resume-prompt's
SQ212A note — so the whole feature went in per §13.18.43 (read in full):
- **Binding:** the FD loop now binds the RECORD clause exactly as the SD loop did (shared `BindRecordClause` —
  ONE binding; the fixed Format-1 `RECORD CONTAINS n` stays null); the DEPENDING name resolves post-build into
  `FileModel.VaryingDependingItem` (the FileStatusItem pattern); `VaryMin`/`VaryMax` default per GR9/GR10 with
  the GR8a occurs-depending MINIMUM-occurrences record size (see RL211A below).
- **Verbs:** WRITE/REWRITE (sequential + keyed) pass the GR13a DEPENDING length; with no DEPENDING the runtime
  takes the image's own length (GR13b/c — an ODO record's image already renders at its current extent);
  READ/keyed-READ store the just-read length back per GR15 (`EmitReadLengthStore`, success-branch only per
  GR12); RELEASE/RETURN handle the now-nullable Depending (the m-TO-n SD form); the SORT USING transfer
  releases at `LastReadLength` (GR12b). GR16a READ-INTO equivalence documented at the call site (the padded
  area makes the implicit MOVE identical).
- **Runtime:** varying connectors LENGTH-FRAME records on disk — SequentialFile gains the 4-byte LE prefix
  framing (the KeyedFrames convention; format is implementor-defined per GR2, self-consistency is what
  matters); the keyed stores already preserved frame lengths. '44' (`RecordSizeViolation`) on WRITE/REWRITE
  outside [min,max] (GR14a / §14.9.35 GR20) and on the record-sequential same-size rule (GR16); relative/
  indexed REWRITE sizes may differ (GR18). `LastReadLength` on all three connectors + the facade.

**3. Two corner-case regressions caught by the suite, both real bugs:**
- **SQ212A's handler end:** DEVLOG 559's CCVS termination-tail accommodation picked the FIRST trivial-exit
  paragraph followed by a STOP RUN — but SQ212A's handler body contains earlier PERFORM-THRU exit points
  (FAIL-ROUTINE-EX1), so the declarative's `GO TO EXIT-PARA` sailed past the bounded end into CLOSE-FILES1 →
  STOP RUN after ONE invocation. The boundary is the LAST trivial-exit paragraph preceding the tail.
- **RL211A's bare `RECORD IS VARYING.`** over an ODO record: the GR9 minimum defaulted to the record's
  ImageWidth — the MAX allocation (140) — so every shorter write '44'd. §13.18.43 GR8a: a record description's
  minimum size sums an occurs-depending table at its MINIMUM occurrences (120 here) — `MinRecordSize` now
  computes that.

**Locked: SQ212A, RL206A, IX106A** (SQ228A/RL211A/RL207A/RL208A were already locked and stay green — now for
the spec-correct reasons). **806 conformance + 15 unit green; 234 NIST programs locked.**

