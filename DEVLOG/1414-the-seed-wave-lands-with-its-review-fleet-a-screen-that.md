## Entry 1414 — 2026-08-31 08:10 PDT — The seed wave lands with its review fleet: a screen that missed the whole TYPE population, a "dead" arm that changes four output lines, and a boundary that was a crash

**PB184 + PB188 land together with the fifteen-finding review fleet folded in.** The wave itself is three
defects: the §13.18.63.3 group-level VALUE syntax rules were enforced nowhere (COBOLNET1702), the §13.18.63.4
GR5 area rule was missing from `GroupImageCodec` — the ONE seeder for every character-image backing since PB164
— and the area rule dropped figurative operand words entirely. The fleet then found that the fix for the first
one reproduced PB184's own symptom inside itself, and that the second one's "no program's answer changes" was
wrong. Both were settled by probe before a line moved.

### The screen could not see a TYPE clause, and that is a design fact, not a filter bug

`CheckGroupValueDeclarations` walked `DataBinder.ConformanceForest`. That forest's final filter is
`StrongTypeModel.TypeAnchor(item) is null`, and `ExpandType` sets `TypeName` on the SUBJECT of the TYPE clause —
so a reference site and its whole clone subtree fell out. Measured, both legs:

```
01 T IS TYPEDEF. 05 A PIC 9(4) COMP. 05 B PIC X(2).   01 R TYPE T VALUE "ABCD".
  → compiled CLEAN, A=0000, B=spaces          (no diagnostic at all)
01 R VALUE "ABCD". 05 A PIC 9(4) COMP. 05 B PIC X(2).
  → COBOLNET1702                              (the byte-identical inline spelling)
```

The violation is COMPOSED — R's entry carries the VALUE and no usage, T's entries carry the usage and no VALUE,
and neither carries it alone. §13.18.57.4 GR3 makes the reference-site VALUE meaningful, and its NOTE sends the
reader to the VALUE clause's own syntax rules for exactly this composition.

⛔ **The fix is a SECOND forest, not a weakened one.** `ConformanceForest` keeps its contract: the WRITTEN-ENTRY
set the per-entry data-attribute gates fire over once each, clones excluded because the template was gated once.
`CompositionForest` adds the clones back. A pass now picks **by what its rule is about** — a per-entry attribute
gate takes the first, a rule about the composed entry takes the second — and each names the other in its doc
comment so the next author has to choose deliberately. Both are one `.Where` off a shared `DeclaredForest` spine.

The second half is provenance. `DataItem.ValueIsCopied` is set wherever a VALUE is transplanted
(`CopyEntryDescription`'s `??=`, `CloneItem`), so the screen answers **once per WRITTEN VALUE clause** rather
than once per reference site. That also fixed a duplicate nobody had filed: `01 A VALUE "ABCD". 05 X PIC 9(4)
COMP. 01 B SAME AS A.` reported COBOLNET1702 twice, both anchored at X's single declaration. (A SAME AS entry
can never carry its own VALUE — §13.16.3 SR12, and the binder does enforce it, COBOLNET1555; probed, so no
separate note is owed.) A third dedup: a subject subordinate to another subject is skipped, because SR13
sentence 2 already rejects that shape once at the outer subject whose walk covers the whole subtree.

### SR1 pulled forward out of PB206 — because it was load-bearing for a rule already shipped

The fleet asked whether the `alphanumericGroup` predicate matched §13.18.29.4 GR3. It did not: GR3 says "not
specified or implied … for a group item **that is not strongly typed and is not a variable-length group**", and
only the first conjunct was written. That mattered because §13.18.63.3 SR1 — which bars exactly those two shapes
from being a VALUE subject — was ALSO unenforced. Measured, both drew COBOLNET1702 naming SR14, a rule GR3 says
does not reach them, while their real violation went unreported:

```
01 ST IS TYPEDEF STRONG VALUE "ABCD". 05 SY PIC 9(4) COMP.
01 GV VALUE "ABCDE". 05 GB PIC 9(4) COMP OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 5.
```

Both now draw **COBOLNET1703** (`group-value-subject-shape`) at the subject, and the subtree walk is skipped —
the entry itself has to change. ⚠ The distinction that had to be measured, not assumed: `OCCURS … DEPENDING ON`
is NOT a variable-length group. §8.5.1.12.1 defines the term over dynamic-**length** elementary items and
dynamic-**capacity** tables only, so an ODO group stays an alphanumeric group item and still draws SR14. An
earlier reading of the probe had it the other way round and would have shipped an under-rejection.

### PB188's "dead arm, no golden" was wrong, and the golden changes four output lines

PB188 shipped the width fix arguing the arm was unreachable: `ValidateValueCategory` meets a quoted literal on a
numeric subject first and either errors or rewrites it. True — of `item.RawValue`. But `InitializerFor` reads
`effRaw = rawOverride ?? item.RawValue`, and `rawOverride` is fed per occurrence by `TableValueInit` from
`BuildTableValueSpecs`: raw text, quotes intact, on a path that never calls `ValidateValueCategory`. The
5143/5143 instrumented green was honest about what it measured — the corpus has no such program — and dishonest
only in what was concluded from it. [[feedback_reachability_is_measured_not_deduced]], on the closing sentence
rather than the premise.

| | `8ca74a3d` | now | derived from spec |
|---|---|---|---|
| `DISPLAY GL` | `AA012012CC` | `AA+012+012CC` | GR12/GR13 + GR5/GR7 + §13.18.52.4 GR6a/GR6b |
| `DISPLAY LB(1)` | `012` | `+012` | " |

`PIC S9(3) SIGN IS LEADING SEPARATE` is the visible case because it is a zoned DISPLAY leaf whose `pic.Length`
(3 digits) differs from its `ImageWidth` (4 — GR6a: the sign "character position is not a digit position"). The
old `StrStore(chars, pic.Length)` seeded three characters into a four-wide window and displaced every following
member. The golden carries the TRAILING SEPARATE twin so a four-wide window cannot be mistaken for a three-wide
one plus a stray sign; every expected value was written down from the clauses before the confirming run, and the
twin — which no probe had measured — matched on the first try.

⚠ That program is NOT conforming: SR2 is an ALL FORMATS rule and the format-2 screen does not exist. That is
PB208's first half, open, and the fixture's header says so in as many words, so the green cannot be read as a
decision that format-2 literals escape SR2.

### PB207 was filed as a silent mis-seed. It was a compiler crash — and the "safe" half was wrong too

```
01 BG GROUP-USAGE BIT VALUE B"1010". 05 B1 PIC 1(2). 05 B2 PIC 1(2).
  → Unhandled System.ArgumentOutOfRangeException in GroupValueSlicer.SliceInit
```

Measured on the wave tree AND independently in a clean worktree at `8ca74a3d`, so the crash predates PB184.
Then the note's other claim — "right for a single-member bit group; only a multi-member one can observe the
gap" — was probed and is false: `05 B1 PIC 1(4).` displayed `1`, one boolean position, against `1010` for its
member-wise twin. The area text is truncated to the group's 1-character `ImageWidth` before it is sliced, so the
single-member case loses three of four bits. Both member counts were wrong; only one was loud.

A bit-bearing group's width is `ceil(bits/8)` laid out by the §8.5.1.6.3 walk and its members do not tile it —
`BG` is 4 bits = 1 character while `B1` and `B2` are 1 character each. So the landing is a **staged loud**
(COBOLNET0899, `bit-group-level-value`) rather than either behavior: a wrong answer that compiles is worse than
a named refusal. PB207's flags moved to `rejects_legal_source` with the crash and the two silent wrong answers
recorded in its body — the frontmatter answers "what does this do to a user's program TODAY".

⛔ **And the exclusion is now written once, keyed on the FACT.** `GroupImageCodec` restated it as
`GroupUsage is not GroupUsage.Bit` — narrower than what it protects. What switches `ImageWidth` to the bit walk
is `HasBitDescendant`; GROUP-USAGE BIT is only the commonest way to acquire it. The restatement is deleted and
both lanes inherit `AreaTextOf`'s one predicate.

### Two citations that were real, checked, and about something else

`§13.18.26.4 GR3` does not exist — §13.18.26 is the FULL clause and has no `.4`. The rule is **§13.18.29.4 GR3**,
and §8.5.2's quoted sentence lives in **§8.5.2.1**. And the figurative area fill is **§8.3.3.6.4 GR2** — the
branch for "the length of the string IS specified in the rules for the context", which names the VALUE clause and
carries both the repeat and the truncate-from-the-right — not GR3a, which is the concatenation-expression rule
and gives length ONE. Anyone re-deriving the golden from GR3a computes a one-character fill and concludes the
golden is wrong. Both had propagated into code, the design SSOT, a fixture comment and the work note before
anyone re-ran `--check` on the clause NUMBER: the inheritance failure mode CLAUDE.md rule 1 names.

### Diagnostic text, and a drift test that failed on its first run

`UsageWord`'s fallback was `usage.ToString().ToUpperInvariant()` — `PROGRAMPOINTER`, `FLOATBINARY32`,
`BINARYCHAR`, `OBJECTREFERENCE`, words no COBOL program contains. It now DERIVES the §13.18.60.2 keyword by
hyphenating the enum name on its case/digit boundaries (18 of 24 members, so a new usage is automatic) with five
explicit spellings, and `UsageWordDriftTests` asserts every member renders as reserved COBOL words. It went red
immediately: `COMPUTATIONAL-1/-2/-5` are not §13.18.60.2 words. FLOAT-SHORT and FLOAT-LONG are; COMPUTATIONAL-5
has no ISO spelling at all and is kept as the dialect word the programmer actually wrote.

### Pins, and the proof they can fail

New: `negative/pb184-group-value-type-reference` (the composed shape), `negative/pb184-group-value-strong-subject`
+ `negative/pb184-group-value-variable-length-subject` (SR1's two arms), `negative/pb207-bit-group-level-value`
(the crash, refused by name), `2023/pb188_table_value_image_width` (the LEADING/TRAILING twins). Four legs added
to `2023/pb184_group_value_area`: `S5`/`C7`/`C8` close the operand-form × lane matrix — `ALL <figurative-word>`
was pinned by neither lane and `ALL "literal"` only by the slicer — and `TV` is the POSITIVE control, an
all-DISPLAY template composed at a reference site carrying a VALUE, which is LEGAL: a widened screen that also
over-rejected would have passed every negative fixture.

Proved able to fail: reverting `CompositionForest()` to `ConformanceForest()` reds the type-reference and
strong-subject fixtures ("must be REJECTED at --std 2002"), then restored. The negative manifest's `_comment`
also stopped promising an `"editions"` map no entry ever carried and no runner ever read; it now describes the
`*> reject-at:` header the runner actually parses.

**Wave-local gate GREEN** — Conformance 1281/1281 · Unit 5091/5091 · characterization 33/33, filter
`~Value|~Group|~Init|~Image|~Corpus|~Negative|~Type|~Redefines|~UsageWord|~Diagnostic|~StorageForm`.
**Battery #39 accrues** and now covers two batches; batch (b)'s uncovered seam is structural — `ExpandType` and
`ExpandSameAs` now write a flag every group-VALUE screen reads, so every TYPE / SAME AS program is in the blast
radius and NIST + GnuCOBOL are where that population lives.

