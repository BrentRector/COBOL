## Entry 1207 — 2026-08-07 19:51 PDT — R05: the "truncation" was §15.33.3 r1 itself — the defects were the missing record, an advisory that forced the six-site funnel, and a directive-word hole the item's own fact discovered

**R05 is LANDED, and its stated defect was not the defect — the pattern is now 14 for 14.** "EXCEPTION-STATUS
silently truncates an exception-name at 31 characters" — §15.33.3 r1 PRESCRIBES the width ("A 31-character,
left-justified, alphanumeric character string that is the exception-name…", cite-checked). The collision is the
standard's own: §8.3.2.1 lets COBOL-2023 words run to 63 while §14.6.13.1.1 leaves the EC-USER-/EC-IMP- suffixes
unbounded, so a 32..63-character level-3 name is legal yet indistinguishable from its 31-character prefix through
this ONE function — checking, declarative selection and WHEN matching all use the full name. The original
Phase-B F6 finding said the real thing: the choice was recorded NOWHERE. So: the runtime stays r1-exact, the
choice is recorded in the deep-dive's Resolved questions, and **COBOLNET1636** (Warning — legal source stays
legal) names the collision at compile time, once per spelling.

⭐ **THE ADVISORY FORCED THE EXTRACTION THE ONE-RULE-ONE-PLACE DOCTRINE DEMANDS.** Where should a warning about
a written exception-name live? Written exception-names resolved at SIX SITES — >>TURN, the F3 USE builder, the
exception-checking PERFORM's WHEN list, RAISE/RAISING, and the two RAISING partitions — four of them carrying
VERBATIM COPIES of the unknown-name (0711) and introduction-gate (0878) diagnostics. Adding the advisory to one
arm would have manufactured the project's most reproducible defect shape (two-arm dispatch, 5× and counting).
All resolution now funnels through **`EcNameResolution`**; 0711/0878 became catalog descriptors;
`EcNameResolutionDriftTests` pins every `ExceptionCatalog.TryGet` call site with its adjudicated reason and
count. The funnel also CLOSED a pre-existing hole its extraction exposed: the USE-site copy guarded the
introduction gate with `Level == 3`, so `USE AFTER EC EC-MCS` — a LEVEL-2 name of a 2023-only family — slipped
through un-gated at 2002/2014 (fact ECT052 pins it). And the drift guard failed ONCE, on its own subject: my
funnel's dedupe spelled "COBOLNET1636" as a bare literal. A guard that has never failed has never been tested;
this one earned its keep before its first commit.

⛔ **AND THE ITEM'S OWN FACT FOUND A DEFECT NOBODY HAD FILED: §8.3.2.1's word-length ceiling never covered
DIRECTIVE-carried words.** The "at 2002 the long word is rejected" fact FAILED when first written — the evidence
ledger's "correctly rejected with COBOLNET1567" had been measured on the RAISE spelling only
(feedback_verdict_evidence_invariant: a missing observation is not a negative one). Words in `>>TURN` and
`>>DEFINE` never reach the tree-walk `VisitCobolWord` funnel: a 44-character exception-name in >>TURN and a
44-character compilation-variable-name in >>DEFINE both compiled CLEAN at --std 2002 — the DEFINE case PROBED
via the CLI, not inferred. **`CobolWordRule`** (Frontend.Common) now owns the ceiling in one place; the
tree-walk funnel, the TURN stage and the DEFINE stage (dialect threaded through `ProcessWithCopy`) report the
SAME rule through the SAME text. 1567 gained the catalog descriptor it never had — `DiagnosticRegistryDrift`
caught my bare literal within minutes of writing it, which is exactly the 1573-collision class it exists to
prevent. `>>COBOL-WORDS` needs no site of its own: the words its literals introduce reach the tree wherever
they are used.

**GATE (wave-local):** Unit **4048/4048** · EC conformance wave green (facts ECT048–ECT052: the r1 31-char
prefix at runtime · the advisory once per spelling across TURN+USE+RAISE · the TURN-only spelling still advised
— the arm a single-site fix would have missed · 1567 at 2002 for the TURN spelling · the closed level-2 hole) ·
DIAGNOSTICS.md regenerated · CLI probes both directions on >>DEFINE. R04's CI run 31234673404: success on all
four jobs.

