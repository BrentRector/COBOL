## Entry 1419 — 2026-08-31 13:04 PDT — Battery #40 stops one row short of green: the flip is a FIX, the compiler is right, and the owed list written to prevent exactly this named a position in its heading that it never measured

Battery #39's stop produced [[PB209]]: a reachability sweep that reported ZERO because it had opened two files
out of a corpus of 1,323. The remedy was a per-bucket, per-case owed list with a derived direction for every
case, written into `kb/Work/PB169.md`, opening with a warning against its own predecessor — *"an owed list that
can absorb unexpected flips is the PB209 defect wearing a new coat"* — and predicting **zero verdict movement,
in every bucket, in both directions**.

Battery #40 returned one flip. The list had no slack, so the flip is unexplained by construction, and this entry
is the attribution.

### The measurement

Four legs green on tree `6f8461d9` (artifacts `scratchpad/battery-40`): FULL greenfield Conformance
**5235 / 5235**, Unit **5106 / 5106**, characterization **33 / 33** concurrent with no host crash, NIST
**353 MATCH / 0 REGRESSION** with the audit CLEAN and the guard verdict **ALL GREEN**. Then:

```
=== DIFFERENTIAL: 1 PER-CASE FLIP(S) ===
    FLIP  syn_subscripts:23  WE_ACCEPT_THEY_REJECT -> AGREE_REJECT  error COBOLNET0844
```

Totals **573/473/206/71 → 573/473/207/70** — AGREE_REJECT +1, WE_ACCEPT_THEY_REJECT −1, consistent with exactly
this one flip and no offsetting pair. 1,323 cases, no NEW/REMOVED, no case without a compiler verdict.

### The compiler is right, and the direction says so before the spec does

This is a `WE_ACCEPT_THEY_REJECT → AGREE_REJECT` flip: a **convergence**, the shape plan §0 defines as a FIX. We
had been accepting something GnuCOBOL rejects; now we reject it too. The case they ship is a NEGATIVE test, and
its `AT_SETUP` title is **"Non-numeric subscript"**. Its shape — described, never reproduced, because the corpus
is GPL — is a `PIC X` elementary used as the subscript of a `PIC X OCCURS 10` table, in both a bare `T(I)` and a
compound `T(I + 1)` position.

Derived from the standard rather than from the diagnostic text, every clause `--check`ed:

- **§8.4.2.3.2**'s general format makes a subscript exactly three alternatives — `ALL`,
  `arithmetic-expression-1`, or `index-name-1 [{+|-} integer-1]`. The item carries no `INDEXED BY`, so the only
  admissible reading is arithmetic-expression-1.
- **§8.8.1.1**: *"An arithmetic expression may be an identifier referencing a numeric data item, a numeric
  literal, the figurative constant ZERO (ZEROS, ZEROES) …"* A `PIC X` item is none of them.
- **§8.5.2.1 Table 2** puts category alphanumeric in class ALPHANUMERIC.
- **§8.4.2.3.4 GR2** — *"The value of a subscript shall be a positive integer"* — is the runtime half, and does
  not rescue the syntactic one.

Before this batch the compiler accepted the program and **digit-decoded the alphanumeric bytes into an occurrence
number**. The flip closes an under-reject. Nothing in `src/` needs to change.

### Attribution: the landing wave had the mechanism written down and still called the reach empty

`ReferenceResolver.ScreenPositionOperandClass`, together with the class question in `ResolveSubscriptName`, is
**new in `1884ba1e`** (burn-down cluster B, [[PB170]]); the symbol does not exist at `9a89fbd1`. Its own doc
comment carries the measurement:

> *Measured on 9a89fbd1: `E(XE)` compiled clean and digit-decoded "0002" to occurrence 2, and so did the
> COMPOUND `E(XE + 1)`*

`E(XE)` and `E(XE + 1)` **is** `syn_subscripts:23`, statement for statement. The wave knew the mechanism
precisely, demonstrated it on a two-statement witness, and then shipped behind an owed list that said no corpus
case could move.

### The defect — [[PB262]] — is that a heading claimed two positions and the evidence measured one

Bucket 8 of the owed list, verbatim:

> ### Bucket 8 — PB170's **subscript / ref-mod** class screen over NON-NUMERIC names. **Expected movement: ZERO.**
> … a scan of all 3,136 programs … found the only **refmod bound** resolving to a non-numeric-class or group
> name to be `syn_move:453 STRUCTURE1(CONST4:2)` …

The heading says *subscript / ref-mod*. The sentence measures *refmod bounds*. Bucket 2 is titled
*"reference-modification bounds that are NAMES"* and enumerates five refmod cases. **Across the entire list the
subscript position is never swept once** — and `ScreenPositionOperandClass` takes a `SegmentPosition` with
exactly two members, so a list with one bucket for a two-armed screen could not have been complete.

That is `feedback_two_arm_dispatch` occurring in the EVIDENCE rather than in the code, and it is
`feedback_measure_the_selectors_complement` again: the sweep is evidence about the positions it LOOKED at, never
about the ones its heading named.

⛔ **And the refutation is a case title, for the second battery running.** PB209's was *"REDEFINES: with
OCCURS"*. This one is *"Non-numeric subscript"*. Twice now a pre-declared empty blast radius has been falsified
by a corpus case named after the exact shape — a keyword search over the case titles would have found both.

This is worse than PB209 in one specific respect, which is why PB262 is MAJOR rather than MINOR: PB209's sweep
was an ad-hoc shell one-liner nobody kept. This list was written **deliberately, as the remedy for PB209**, and
it warned in its own opening that slack "will swallow up to 32 genuine regressions without anyone noticing". The
one thing that worked is that it labelled bucket 8 as **inherited rather than re-measured** — and the row it
flagged as weak is the row that broke. Honest labelling caught what the measurement missed.

### Blast radius, re-measured with the instrument PB209 built

`corpus_sweep.py --codes COBOLNET0844` over the 1,323 cases the differential actually compiles (census printed
every time: conformance 1000 · nist-programs 459 · nist-copylib 53 · characterization 17 · version-matrix 0 ·
differential 0 · gnucobol-external 1,611 = **3,140 programs**) returns three cases: `syn_multiply:28` and
`syn_multiply:102`, both AGREE_REJECT before and after — unchanged controls — and `syn_subscripts:23`, the flip.
The external blast radius of the new screen is exactly one case, and it converged. The internal corpora answer
the same question by compilation, and all four legs are green.

### Why this stops rather than rebaselines

The standing order for this battery: any per-case flip is unexplained and must be attributed before the battery
is called green, because the owed list has no slack to absorb one. The verdict is correct and the flip is almost
certainly one the baseline should encode — but regenerating the baseline in the same breath that discovers the
owed list was wrong would destroy the only record that it happened, which is the [[PB209]] precedent verbatim.

So: `tests/external/gnucobol-verdict-baseline.tsv` is **UNCHANGED**. Battery #40 is **not** recorded as a green
reference and battery #39 remains the current one. The two director-authorized hygiene riders that were to ride
with a green recording — the four YAML-invalid `kb/Work` titles and the CLAUDE.md rule-8 `inventory_rows`
sentence — are **not applied**; they were conditioned on green, and a stop is not green.

The population arithmetic is reconciled by test NAME rather than by subtraction, because a count delta cannot see
an add-one/drop-one pair: Unit **5091 → 5106**, **zero removed**, +15 added across five classes — 3
`ExternalCorpusPopulationDriftTests` (PB209, which landed after #39's Unit leg), 6 `OperandContextRulesTests` +
2 `OperandWalkCoverageTests` + 1 `ParenTokenTwinDriftTests` (cluster B), 3 `DefectiveRowCoverageDriftTests` (the
registration wave). Conformance **5152 → 5235**.

### One run-level incident, named rather than smoothed over

The battery's FIRST invocation died in phase 0 with 146 compiler errors, all cascading from one line:
`CSC : error CS0009: Metadata file '…/CobolSharp.Runtime/obj/Debug/net10.0/ref/CobolSharp.Runtime.dll' could not
be opened -- PE image doesn't contain managed metadata`. The file was **49,664 bytes of pure NUL** — a corrupt
gitignored intermediate, not a source defect. It was cleared, the solution rebuilt clean, and **all 1,912 built
assemblies in the tree were then scanned for a valid `MZ` header (0 bad)** before the battery was re-run, so the
repair is not being taken on faith for the one file that happened to announce itself.

