## Entry 1466 — 2026-09-04 23:18 PDT — Lane-3 batch b3 registered: 226 rows over the eight DATA DESCRIPTION clauses, 95 new notes, and a CONFORMS share of 15.9 % — §13.18 is a far more defective clause family than §14, and the yield number PB469 wanted now has three points

The third lane-3 adjudication batch — §13.16 (the data description entry) · §13.18.8 BLANK WHEN ZERO · §13.18.34 LINAGE · §13.18.40 PICTURE · §13.18.52 SIGN · §13.18.55 SYNCHRONIZED · §13.18.60 USAGE · §13.18.63 VALUE, seventeen subject files, every one adjudicated AND refuted — is on main. It is the largest single movement of the register so far: **PB486–PB580**, ninety-five new notes, plus five extensions to notes that already owned the mechanism.

### What was recorded

| verdict | rows |
|---|---|
| PARTIAL | 116 |
| DIVERGES | 38 |
| NOT-IMPLEMENTED | 36 |
| CONFORMS | 36 — **16 close the row**, 20 are CONFORMS-but-untested |

226 rows recorded; **GAP 3005 → 2992**. Four more were adjudicated and deliberately NOT recorded (below). The twenty CONFORMS-but-untested rule-ids are listed in `kb/Work/PB370.md` for the golden lane's round 4, with a caveat the golden writer has to read first: `SR-13.16.3-20` lost its cited witness to the refuter (the negative golden pins the SUBORDINATE spelling, not the level-1/77 one the rule governs) and needs a NEW witness rather than a re-cite, and five of the twenty are IMPLEMENTOR-DETERMINATION rows whose golden must pin the determination in `docs/CONFORMANCE.md` as well as the measured layout — for §13.18.52 that documentation does not exist yet (`PB160`).

### ⭐ THE DEFECT YIELD — the plan's softest number (PB469), now with a third point

| batch | clause family | rules | notes opened | notes/rule | CONFORMS share | GAP delta |
|---|---|---|---|---|---|---|
| b1 | §14 statements | 184 | 85 | 0.46 | — | −15 (8 %) |
| b2 | §14 statements | 292 | 84 | 0.29 | 78/295 = **26 %** | −32 (11 %) |
| **b3** | **§13.16 + §13.18 data description** | **226** | **95** (+5 extended) | **0.42** | **36/226 = 15.9 %** | **−13 (5.8 %)** |

Two things fall out of that row and neither was predictable from §14.

**The data description clauses are roughly twice as defective as the statements.** A §14 batch closed a quarter of its rules outright; this one closed a sixth, and its GAP delta per adjudicated rule is HALF b2's. The reason is visible in the notes rather than in the numbers: §13.18's rules are overwhelmingly about the SHAPE of a declaration — which clauses may be combined, what a group-level clause does to its subordinates, what a picture character-string may contain — and this compiler's data binder is a per-entry `foreach` over clause kinds. It has almost no machinery for a rule whose subject is a RELATION between entries, so the whole class of "applies to each elementary item in the group" rules lands on the same missing arm.

**The yield estimate should be revised UP for the data division and the projection re-run.** 0.36 notes/rule was fitted on §14; §13 returns 0.42 even after aggressive clustering (104 adjudicator findings + 39 refuter mechanisms → 95 notes, a 40 % reduction by merging one root across files). The fix lane's queue is now 636 work items.

### The refutation ran HOTTER than b2's, and in one direction

74 refuter checkpoints, one per adjudicated CONFORMS: **35 upheld, 39 overturned — 52.7 %**, against b2's 36.7 %. Every overturn was downward. Thirty-six became PARTIAL or DIVERGES; one kept CONFORMS while its test-ref was CLEARED; and the remaining flips are the four rows below. The refuters were not merely stricter — they were finding a REPEATED shape the adjudicators kept missing, and it is worth naming because it will recur in every remaining data-division batch: **the adjudicator measures the clause on the entry that WRITES it, and the refuter flips to the entry that INHERITS it.** Nine of the thirty-nine overturns are exactly that (`SR-13.18.40.3-31`, `SR-13.18.60.3-5`, `GR-13.18.60.4-11/-12/-13/-14/-15/-21`, `SR-13.18.63.3-24`), and they collapse into two notes.

### The four rows that were adjudicated and NOT recorded — and a structural rule so it never has to be judged again

`SR-13.18.63.3-36/-38/-39` were adjudicated CONFORMS and refused by the refuter on two grounds: the row already carries `DOCUMENTED-NON-SUPPORT`, which is an OWNER verdict (D13) owned by PB260/PB261/PB281–PB283 and not this lane's to write; and the CONFORMS reasoning rested on "the antecedent cannot be created because Format 5 is refused", **which is false as measured**. `merge_batch.py` had a spelling-based guard for a refuter withdrawal and did not recognise this one, so it would have downgraded three closed owner rows to PARTIAL. Fixed at source.

Then the SAME batch carried a FOURTH, which no refuter saw because that row was never adjudicated CONFORMS: `GR-13.18.63.4-24`, `DOCUMENTED-NON-SUPPORT → DIVERGES`. Downgrading an owner verdict looks harmless — it fakes no closure — but it still moves the burn-down denominator on an agent's say-so and detaches the row from the notes that own the declined modules. So the rule is now STRUCTURAL rather than a spelling: `merge_batch.py` reads the inventory and refuses any record whose row already carries an owner verdict, in EITHER direction. A hand-listed exception would have caught three of the four; a structure catches the next one too (CLAUDE.md rule 5).

The measurement those four rows rest on is real and is filed as a defect: **the A.4.14 VALIDATE decline LEAKS.** The printed Format 5 figure brackets the connective — `[ IS / ARE ] { INVALID / VALID }`, and SR39 is the rule that makes it optional — so `VALUE literal VALID.` with no IS/ARE is legal Format-5 source. `| VALID` is an UNGUARDED `cobolWord` alternative (`CobolWords.g4:106`), so the greedy VALUE operand loop eats VALID as a constant-name operand, the Format-5 tail never matches, and the unresolved operand reaches the backend as a bare C# identifier — a raw Roslyn error, not a decline. The owner question is in `kb/Work/PB579.md`: **does a DOCUMENTED-NON-SUPPORT row stand while the decline that justifies it has a measured hole?**

### The mechanisms, clustered

Ninety-five notes is too many to list, so here are the roots that own the most rows — each one a single mechanism the fix lane can take whole:

- **PB486** — `dataDescriptionClauses : dataDescriptionClause*` is a bare star: every clause may be written twice and in any order, so §13.16.3 SR4's two POSITIONAL obligations (REDEFINES and TYPEDEF immediately after the entry-name) are checked nowhere, and duplicate SIGN / SYNCHRONIZED clauses are last-wins. Three findings across three subjects, one root.
- **PB566** — `InheritUsageClauses` implements §13.18.60.4 GR1 as a narrow rewrite built for USAGE INDEX. A picture-less elementary under a group-level `BINARY-SHORT` / `FLOAT-LONG` / `FLOAT-BINARY-32/-64` resolves to a **zero-length group** with no diagnostic (`FUNCTION BYTE-LENGTH` = 0 where the rule fixes 2, 8, 4, 8), and the USAGE BIT cell is accepted silently. Eleven rows.
- **PB570** — the `WITH NO SIGN` phrase is read from the entry's OWN `usageClause`, so a group-level `USAGE PACKED-DECIMAL WITH NO SIGN` loses BOTH its §13.18.40.3 SR31 screen and its §13.18.60.4 GR11 storage effect: the inherited leaf keeps the SIGNED layout, one byte too wide per item, silently. A 2023 record laid out this way will not interchange.
- **the national-form population** — one note collects every row whose obligation is undelivered because national-form data is refused by name (`COBOLNET0899`, "Phase 4a residue"): a national-form boolean, a national-form numeric or numeric-edited item, a national-edited item, and a national leaf in a file record. `PIC N(3)` in an `FD` record does not merely restrict where the category may be USED — the data description entry itself fails to compile, so §13.18.40.4 GR9's DEFINITION is unavailable in the section where national data is most often declared.
- **the level-88 pre-filter** — `BindEntries` does `if (lvl == 88) { …; continue; }` BEFORE `BindEntry`, so NO data-description clause on a level-88 entry is screened at all. That is why `88 C IS TYPEDEF VALUE 'ABC'.` compiles clean at 2002/2014/2023 with the condition-name still evaluating, and why the USAGE screens never see a level-88 conditional variable.
- **`BindCondition`'s Format-3 guards read `parent.Pic`** — and a BIT or NATIONAL GROUP conditional variable has none, so the SR10 size limit, the class funnel and the national guard all fall through on `null`: an 8-bit boolean literal on a 4-bit group compiles AND RUNS, where its elementary twin draws `COBOLNET0898`.
- **`InitializeBinder.InitializeSender` qualifies a receiving operand on `DataItem.RawValue` alone** — so a Format-2 table VALUE and a pointer/object-class VALUE are invisible to the entire INITIALIZE lane, and the linkage, external and BASED occasions on which §13.18.63.4 GR3/GR4 say a VALUE takes effect never deliver.
- **the ALIGNED clause has NO grammar rule at all.** `grep -rn ALIGNED src/Cobol.Net.Frontend/Grammar/` is empty; `genericClause : IDENTIFIER (IDENTIFIER|literal)*` swallows it as vendor noise. `05 B2 PIC 1(3) USAGE BIT ALIGNED.` compiles clean and changes nothing — §13.18.1.4 GR1's byte-boundary alignment is accepted and inert. The same swallow eats any unrecognized clause word run, which is filed on its own row.
- **the PICTURE EDITING phrase's grammar demands a QUOTED literal** where the printed general format (rendered from the PDF, not read from the transcription) writes `EDITING character-1` BARE. Every conforming EDITING phrase is a parse error, and the SR10/SR11 checks that DO exist are reachable only through a spelling the standard does not define.
- **PB579** (`status: owner`) — the standard decimal floating-point usages are refused by name under Annex A.3 item 19 with §4.2.6 licence and a `CONFORMANCE.md` row already written; four rows are recorded NOT-IMPLEMENTED only because `DOCUMENTED-NON-SUPPORT` is the owner's verdict to give.

### What did NOT hold, and what the registrar had to repair

Every finding was re-probed on this worktree's own build before it became a note — a 1,942-program sweep of the adjudicators' probe corpus at `--std 2023` plus targeted re-probes for the mechanisms train 1's `PB203` (bit-usage groups under REDEFINES) could plausibly have moved. **Nothing stopped reproducing.** The BLANK WHEN ZERO under-rejects still print `A2[0000{]` / `A3[00000]` / `A4[000000]`; the group-USAGE zero-length groups still measure 0; the national REDEFINES receiver still draws `COBOLNET0899`; the picture-less inherited USAGE BIT item still compiles clean. Seven notes carry that measurement verbatim.

Three things in the adjudicators' own output did not survive contact with the schema, and all three are recorded because they will recur:

1. **204 of 226 records wrote FORENSIC PROSE into `code-location`** — line numbers, parentheticals, whole sentences — where the schema wants `<repo-relative-path>[#Symbol]`. Normalised, with the original text moved verbatim into `notes` rather than dropped.
2. **62 code-locations used a TYPE-QUALIFIED fragment** (`CobolEdit.cs#CobolEdit.Format`). The gate resolves a fragment by word search over the file body, and `CobolEdit.Format` is not a string that occurs in `CobolEdit.cs` — the inventory's own convention is the bare member (`IntrinsicSignatures.cs#Abs`). Stripped, and then each fragment was PROVED present before shipping; the ones that could not be proved were dropped to the bare path rather than recorded as a phantom.
3. **28 test-refs were NAMESPACE-QUALIFIED** (`conformance-test:CobolNet.Tests.Conformance.LinageConformanceTests.Gr1_…`), which names a file that cannot exist. All 31 unresolved refs were this one shape.

And on the citations themselves (CLAUDE.md rule 1): every quotation in every new note was put to `cite.py --check` by the registrar rather than inherited. **62 validated verbatim; 20 did not, and 18 findings quoted no rule sentence at all.** Those 38 notes carry an explicit ⛔ line saying the MEASUREMENT stands but the QUOTATION is the finding's paraphrase and must be re-derived before it reaches a code comment, a golden or this log. That is the inherited-citation failure mode the rule is about, caught at the register rather than three landings later.

One misfiled citation was found by `audit_doc_citations.py` and repaired: a §13.18.60.4 GR11 sentence sitting after a §13.18.40.3 SR31 mention, which the prefix heuristic attributed to the wrong clause.

### Dossier gaps — 17 more subjects, and they are four shapes, not seventeen complaints

`PB372` gains all 17 returns plus the synthesis: (1) a citation-keyed selector is structurally blind to an ABSENCE, and an absence is where the GAP is — LINAGE's four syntax rules have no implementation, so nothing cites them and the dossier's source list is empty exactly there; (2) the grammar selector returned FD/SD productions for four subjects because `linageClause`'s comment cited `ISO 13.16` — a citation `PB159` already REPAIRED, so the dossier snapshot predates its own fix; (3) the line numbers are stale by ~25 lines; (4) the register and diagnostics selectors return empty where an owner exists — LINAGE got an empty `register.notes` while eight notes match the keyword. A fifth, narrower one worth fixing alone: the TESTS list offered a `*_MatchesLegacy` differential as a §13.18.8 witness, and a selector that offers a differential is worse than one that offers nothing.

### Gate

`dotnet build CobolSharp.sln -c Debug` clean; the inventory battery **55/55 passed** (`SpecTraceabilityInventory` · `DefectiveRowCoverage` · `DerivedVerdict` · `AnnexA1Register` · `DerivationRegister` · `AnnexA2Undefined`); `audit_code_citations.py --check` 0 findings; `audit_doc_citations.py --check` 0 misfiled; `semgrep/verify.py` PASS; `work.py check` ✓ 636 work items, all well-formed.

