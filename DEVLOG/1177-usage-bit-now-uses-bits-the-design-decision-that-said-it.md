## Entry 1177 — 2026-08-04 23:00 PDT — USAGE BIT now uses bits; the design decision that said it needn't had cited a rule that says something else

**PB43 landed, and it started as PB24's last open sub-item — which turned out not to exist.** PB24 recorded
"§15.50.4 r9, the rounding step: MEASURED AND VIOLATED", with the fix shape "compute group length in BITS and
convert with a ceiling". Deriving the rule before reading the code — the `spec-lookup` order, which exists for
exactly this — dissolved the diagnosis, the fix shape, and the location, while leaving the symptom standing.

## ⛔ THE DESIGN DECISION RESTED ON A RULE THAT SAYS SOMETHING ELSE

`PicInfo.Usage.Bit`'s own comment: USAGE BIT "maps to the SAME one-'0'/'1'-character-per-position string storage
as a display-form boolean item (the §13.18.40.4 GR14 R14 representation license, D-B1)".

GR14 is real and says what the comment says: *"Each boolean character can be represented in storage as a bit, an
alphanumeric character, or a national character."* **But GR14 lists the AVAILABLE representations; the USAGE
clause SELECTS one**, and both selections are mandatory:

- **§13.18.60.4 GR5** — *"The USAGE BIT clause specifies that **bits shall be used** to represent a boolean data
  item. A data item described with USAGE BIT is a bit data item. The alignment … is specified in 8.5.1.6.3."*
- **§13.18.60.3 SR13(b)** — when no USAGE clause is written, USAGE DISPLAY is implied; **GR7** makes DISPLAY
  "an alphanumeric coded character set".

So plain `PIC 1(n)` char-per-position is **required** (COBOL.NET was right), and `USAGE BIT` bits are **required**
(COBOL.NET was not). Two storage forms for one category is not the two-mechanisms anti-pattern — it is what the
USAGE clause is *for*. And §8.5.1.6.3 exists solely to align items that occupy bits: a clause with nothing to say
if USAGE BIT were char-per-position. D-B1 called its choice "PERMANENTLY conforming"; the word to distrust in a
design doc is *permanently*.

## ⛔ §8.5.1.6.3 IS A LAYOUT, NOT A SUM — AND THAT KILLED PB24'S STATED FIX

The discriminating case, measured:

```cobol
05 S1 PIC 1(3) USAGE BIT.  05 S2 PIC X(1).  05 S3 PIC 1(3) USAGE BIT.
```

"Sum the bits then round once" gives `3 + 8 + 3 = 14 bits → 2`. The standard requires **3**: a bit item
*immediately following an elementary bit item of the same level* takes the next bit position (they share a byte),
and **any other** bit item is "aligned at the first bit position of the first available byte". The character item
between them breaks the adjacency, so each 3-bit run takes its own byte. Position depends on the previous
sibling's kind and level, which no sum can express. **Building from PB24's shape would have replaced one wrong
answer with another and shipped a golden pinning it.**

Nor did the fix belong in `FUNCTION LENGTH`: `LENGTH` and `BYTE-LENGTH` both reported 8 and were internally
CONSISTENT — the item really did occupy 8 character positions. Making LENGTH answer 4 while storage stayed 8
would have been a rule-4 workaround, LENGTH lying about its own storage.

## ✅ WHAT LANDED (design D19, superseding the USAGE BIT half of D-B1)

The architecture was already in the tree — **V59's numeric byte forms**. A `PIC 9(4) COMP` holds its VALUE in a
native `long` and occupies its BYTE form in the record image; COMP-3 images as BCD. The C# carrier is not what the
standard constrains; sizes, offsets, overlays and the image are. So a bit item keeps its `'0'`/`'1'` carrier —
every MOVE/compare/ref-mod path untouched — and gains a bit width and a bit-granular offset.

`BitLayout.ExtentBits` is the ONE §8.5.1.6.3 walk. `ImageWidth`/`ByteWidth`/`RecordLayout` route through it **iff
the subtree has a USAGE BIT leaf**, so a bit-free program keeps byte-identical widths *by construction* — the
proof the change is inert for the whole pre-existing corpus, not a hedge.

**§15.50.4 r1 needed its own arm, and why is the useful part.** An elementary boolean item's LENGTH is in BOOLEAN
positions; the generic fold reads `ImageWidth`, now its OCCUPANCY. While USAGE BIT was char-per-bit those were the
same number, so r1 and r3 could not be told apart and one arm answered both. **The arm was not missing — it was
unnecessary**, and giving bit items their true occupancy is what made it necessary.

**And the image had to move, or LENGTH would have lied.** After the width fix alone, `FUNCTION LENGTH` said 4
while the group still imaged as `ABC10101` — 8 characters. Sizes right, image wrong: worse than the consistent
wrong answer it replaced, and not shippable. `CobolBits.Pack`/`Unpack`/`Slice` plus a `Physical.BitRun` in the
emitter pack a run into its bytes, high-order bit first (§8.5.1.6.3 numbers from "the first bit position"),
trailing filler zero. A run spans several FIELDS because same-level bit items share a byte, so the leader carries
the run's whole width and emits one `Pack`; continuations carry width 0 and the group's image width stays a plain
sum. `MOVE W-MIX TO W-IMG` transfers `"ABC"` + the byte **168** (`10101000`), and moving it back restores both
fields.

⚠ **A two-run group failed to compile the first time** — `CS0128`, because each run emitted `var __bits` and a
character item between two bit items makes two runs in one method scope. Named for the run's offset now. Worth
recording because it is the shape the design predicted (runs are per-sibling-stretch, not per-group) showing up
as a compile error rather than as a wrong answer.

⚠ **The sweep then found the OTHER image path** (rule 4 — every bug is a pattern). `AsImage`/`FromImage` were
packed, but `ImageInitOfOne`, the compile-time seed for a **Tier-B REDEFINES backing**, still composed a bit
member as its `n`-character carrier — and the backing is sized from `ImageWidth`, now `ceil(n/8)`, so the seed
would have been silently truncated against it. Two paths compose the same bytes and must agree. Found by asking
what else builds an image, not by a failing test; no test covers a Tier-B class over a bit member.

## ⚠ A GREEN UNIT TEST HAD PINNED THE DEFECT

`Boolean_Pic1x4_ImageWidthEqualsLength_CharImageBoolean` asserted `ImageWidth == 4` for `PIC 1(4) USAGE BIT`,
justified as "§13.18.60.3 SR5/SR13b make DISPLAY and BIT the same string storage". Neither cited rule says that:
SR13(b) applies only when no USAGE clause is written, and SR5 merely requires a bit item to carry a boolean
picture. A green test resting on a misreading reads as a decision — `feedback_green_test_can_hold_a_gap_open`.
Replaced with one test pinning BOTH usages and a second pinning the split-run layout.

## ⚖ AND ONE OF MY OWN CLAIMS DID NOT SURVIVE, THE SAME MISTAKE ONE LEVEL UP

I first read `LENGTH` of `01 W-B. 05 B1 PIC 1(5) BIT. 05 B2 PIC 1(3) BIT.` = 8 as CORRECT under §15.50.4 r1,
"a bit group item returns boolean positions". **W-B is not a bit group item.** §13.18.29.4 GR3: a group with no
GROUP-USAGE clause specified or implied "is an alphanumeric group item"; §13.16.4 GR1 implies GROUP-USAGE BIT only
for a group *subordinate to a bit group*. So r3 applies — character positions — and the two items share one byte:
the answer is **1**. Reading "bit group item" as "a group of bit items" is the same nearest-general-sentence error
that produced D-B1, one level up, made by me while writing the note that diagnosed it.

## 🔬 THE BATTERY CAME BACK RED, AND THE RED WAS THE GATE'S OWN (PB44)

`=== BATTERY: NOT GREEN (rc=1) ===` on **1 failed of 3647** unit tests, with everything else green: Conformance
**4201/4201**, characterization **33/33**, `guard-fast` **ALL GREEN**, and the differential **0 per-case flips**
— the first PB43-era run through the new committed baseline, so that zero is measured rather than inferred.

The failure was `GrammarDiagramGeneratorDriftTests.Generator_RunsClean`:

```
Exception calling "ReadAllText": The process cannot access the file 'E:\Temp\ccf-…\CobolScreen.md'
because it is being used by another process.
```

`gen-grammar-diagrams.ps1` shells out to `rr.war` and immediately reads the file java just wrote; java has exited
but its handle is not always released when PowerShell resumes. Serial re-run: **PASS**. Unrelated to this wave —
the generator touches ANTLR fragments, not the data model — and that was CHECKED, not assumed.

⚖ **Fixed rather than filed.** §0 already warns that a high-JOBS leg can false-red and says to re-run the named
test serially, which is right and not sufficient: **a gate that can go red without a defect teaches everyone to
discount reds**, and the next real regression arrives looking exactly like this one. The read retries with
backoff now. Recorded as PB44.

**Named residue:** `GROUP-USAGE BIT` (§13.18.29) is still unmodelled, so a *declared* bit group stays rejected;
a sub-byte REDEFINES overlay is refused rather than rounded. Both on PB43.

