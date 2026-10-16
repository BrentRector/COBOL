## PB1099 Q1: the RECORD KEY / ALTERNATE RECORD KEY SOURCE phrase is provided (a record key is a list of byte windows)

kb/Work PB1099 (wave 1095, group I). The owner rule R65 (2026-09-30) classified Q1 — Annex A.3 item 40, the
`record-key-name-1 SOURCE IS { data-name-2 } …` key form of §12.4.5.12.2 / §12.4.5.6.2 — as a processor-dependent
element this processor CAN provide, so the COBOLNET1954 refusal (kb/Work PB358) had to become the feature.

**Re-probe.** On the w1095i build `negative/pb358-record-key-source` drew COBOLNET1954 at 85, 2002 and 2023, as
the note said.

**The mechanism — one key shape.** §12.4.5.12.4 GR2 / §12.4.5.6.4 GR2: "Record-key-name-1 defines a record key
consisting of the concatenation of all occurrences of data-name-2 in the order specified" (`cite.py --check` OK on
both). Every layer that carried a key as ONE contiguous byte window now carries a LIST of windows end to end, and
data-name-1 is the one-window case rather than a separate path (the shape kb/Work PB293 recorded as the cost):
- `FileModel.RecordKey` (the resolved key: segments, record-key-name, `CategorySource` for §12.4.5.12.3 SR5 /
  §12.4.5.6.3 SR6), `RecordKeySource` / `KeySourceOperand` (the phrase as written); `PrimeKey` replaces
  `RecordKeyItem`, `AlternateKeys` carries `RecordKey`s, and `RecordKeyClauseWritten` is gone (both forms name the
  clause now). `DataBinder.CaptureRecordKeySource` is ONE capture for both clauses; `ResolveFiles` resolves each
  data-name-2 by data-name-1's SR2 selection.
- Runtime: `KeySegment` (offset, length, layout); `IndexedConnector.KeyOf` concatenates through the one slice;
  `RegisterIndexed` / `AddAlternateKey` take `KeySegment[]`; `FixedFileAttributes.KeyDescriptor` holds the segment
  list (value equality); the store header is FORMAT VERSION 5 (each key's segment count and windows — §12.4.5.12.4
  GR3 makes every window a fixed attribute). A version-4 store is Foreign ('39'). The changed public members make
  this an incompatible runtime surface, so the runtime MAJOR is raised 5 → 6 (Directory.Build.props) and
  `RuntimeAbiPinDriftTests` re-pinned: a module compiled against runtime 5 is refused by `RuntimeAbi.Skew` rather
  than failing inside a key registration.
- `FileControlKeyRules` yields ONE OPERAND PER data-name-2, so every rule stated of "data-name-1 and data-name-2"
  (SR1, SR2, SR3, the minimum-size rule) reaches each through the data-name-1 row; new rows for SR2's last sentence
  ("All occurrences of data-name-2 shall be of the same category") on both clauses; §12.4.5.6.3 SR4's SOURCE
  exemption is now `CoincidingKey`'s first test; the clause-level rows ask only the clause's first operand.
- Naming the key: `FileModel.RecordKeyNameCandidates` (with §8.4.2.2.2 Format 6's one file-name qualifier) is the
  ONE lookup READ … KEY (§14.9.30.3 SR11), START … KEY (§14.9.41.3 SR7; GR15/GR17 b) through
  `BoundKeyedStart.NamedKey`) and COLLATING SEQUENCE OF (§12.4.5.7.3 SR5) ask, as a COUNTED set; another file's
  record-key-name is refused naming the rule. The data-item arms (`RecordLayout.KeyIndexAtOffset`, START SR6 b) 1. "defined without
  the SOURCE phrase") pass over a SOURCE-form key. SORT/MERGE GIVING an indexed file (§14.9.40.3 SR9) compares a
  SOURCE-form prime key only when its windows are one run. The EXTERNAL file fingerprint (§12.4.5.3 GR1 j)/k))
  carries each data-name-2's entry.
- Edition: a post-1985 form gated ONCE on the shared grammar rule, `record-key-source-2002` (VCR row 7.36, a
  derived provisional 2002 edge — Annex E lists no 2014→2023 change, X3.23-1985 prints data-name-1 only).
  COBOLNET1954 is RETIRED (a retired code stays retired); `QualifiedNameClasses`' qualified-record-key-name row is
  Resolved.

**Goldens.** `conformance:2002/pb1099_record_key_source` — a prime key written in the REVERSE of record order and a
duplicate alternate key in the optional-word minimum spelling: the concatenated uniqueness '22', the sequential
order AA02 < MM01 < ZZ01, READ KEY on both keys (one Format-6 qualified), START > on the alternate (02 then 00 in
release order), START WITH LENGTH 2 on the concatenation, DELETE, and START with no KEY phrase; every expected line
derived from the cited rules in its header. `conformance:negative/pb1099-record-key-source-85` (COBOLNET0900 at
85). `FileControlKeyRuleSpecTests`' SOURCE section replaces the decline tests: the spellings compile from 2002,
each data-name-2 rule reports once at its operand, SR11/SR7/SR5's other-file refusals, and the complement. The
five `negative/pb358-*-source*` decline witnesses are removed; ~20 runtime unit tests take the `KeySegment[]`
registration.

**Register.** PB1099 → landed. Batch `w1095i-pb1099-record-key-source` re-verdicted 19 rows: SR-12.4.5.12.3-5,
SR-12.4.5.6.3-6, GR-12.4.5.12.4-2, GR-12.4.5.6.4-2 and SR-14.9.41.3-7 DOCUMENTED-NON-SUPPORT → CONFORMS;
SR-12.4.5.7.3-5 is now tested (the `DRV-SR-12.4.5.7.3-5` derivation, whose antecedent was "no accepted program can
declare a record-key-name", is withdrawn); the rest gained witnesses and lost the retired ones. The
`record-key-source-only` derived-verdict selector is withdrawn. Q3 (§8.13 external repository) is carried by
kb/Work PB1086 and its slices, which already claim DOC-A.1-161/162. Docs: CONFORMANCE.md §2 row 40 Claimed,
COBOLNET_FILES_DESIGN.md "THE SECOND KEY FORM" and D10 (format version 5), the grammar's comments, README.

**Branch review, round 1 (three findings, all fixed by the review finisher).**
- R1-1: `DataBinder.CollatingKeyNamed`'s one-fault-one-verdict guard took `AlternateKeyClause.Item is null` to mean
  "unresolved", and `Item` is always null for the SOURCE form, so `COLLATING SEQUENCE OF AK IN IXG` (IXG another
  file) compiled clean and the clause was dropped. `FileModel.DeclaresUnresolvedKeyNamed` now holds the guard: a
  SOURCE clause is unresolved only when one of its operands is, and matches only under Format 6's qualifier. The
  probe draws COBOLNET1582 (§12.4.5.7.3 SR4/SR5).
- R1-2: the record-key-name lookup returned the FIRST match, the kb/Work PB978 shape. A record-key-name declared by
  two clauses of one entry (Format 6's only qualifier is the file-name both share) bound silently to the prime key
  (READ returned '23' on a stored alternate key). `cite.py --check 8.4.2.2.3 "For each non unique user-defined name
  that is explicitly referenced, uniqueness shall be established through a sequence of qualifiers that precludes any
  ambiguity of reference"` → OK 1). `RecordKeyNameCandidates` returns `NameCandidates<KeyNameReading>`, and the
  positions that admit data-name-1 OR record-key-name-1 (`KeyedIoBinder.RecordKeyNameOf`, `CollatingKeyNamed`)
  count every reading in one set — every visible file's record-key-names, and the data-name reading, since
  §8.3.2.2 rule 3 lets one word be both — for the one verdict, `UniqueOrReportAmbiguous` (COBOLNET1639). The
  sibling sweep found the same "asked first" order in both positions and the cross-file and data-name collisions,
  and all are now in the count. `NameCandidates` gained `Plus` and `Select`, still with no indexer.
- R1-3: five inventory rows still argued from the retired COBOLNET1954 decline. They were restated with evidence:
  GR-14.9.41.4-13 has a new golden `2002/pb1099_record_key_source_national`, where START LENGTH counts national
  positions across the SOURCE segments (LENGTH 3 crosses the IX-B/IX-A boundary, and each row is chosen so that a
  byte count would pick a different record). SR-12.4.5.12.3-4 has a new SR4 witness per data-name-2.
  GR-12.4.5.3-1 has the `ExternalFileEntryIdentityTests` j)/k) SOURCE rows and an operand-order row.
  SR-8.4.2.2.3-1 and FMT-14.9.41.2 were also restated. The deleted lookup was retired as a witness from
  SR-12.4.5.7.3-5 and SR-14.9.41.3-7.

**Branch review, round 2 (one finding, fixed by the round-2 review finisher).**
- R2-1: R1-2 counted the data-name reading through `ReferenceResolver.Probe`, which answers only a unique item. So
  the count went DOWN as same-spelled data items were added: one WORKING-STORAGE `RK` beside the record-key-name
  `RK` drew COBOLNET1639, and two (`RK` in `G1` and in `G2`) compiled clean with the READ bound to the SOURCE prime
  key. Measured on the merged build before the fix: `q1.cob` exit 0, `q0.cob` two SR1 errors. `cite.py --check
  8.4.2.2.1 "No other name has the identical spelling"` → OK 1). `KeyedIoBinder.RecordKeyNameOf` now adds every
  `DataBinder.QualifiedCandidates` survivor (the set the data-name resolution counts) to the `KeyNameReading` set;
  `q1.cob` draws COBOLNET1639 with 3 declarations on the READ and the START, `--permissive` warns. The SR1 theory
  gained the DATA2 rows and now asserts each refusal's declaration count. `CollatingKeyNamed` already counted
  `QualifiedCandidates`, so it had no such hole. SR-8.4.2.2.3-1's notes were restated (batch
  `w1095i-review-r2-pb1099`; CONFORMS, GAP 78 → 78). The merge of main brought PB2758's runtime surface change under
  this branch's major 6, so the runtime surface hash was re-pinned (21C3F112DD1E124B).

**Self-review.** Architecture: one key shape, one capture, one lookup, one gate; no C# text in a bound node.
Code: each cited rule re-read for the operand it names (data-name-1 vs data-name-2, record-key-name-1). Performance:
the one-window key keeps its single-slice path; only a multi-window key allocates its concatenation. Duplication:
`KeyWindow` (the emitter's per-item window reader) is reused per segment rather than copied.
