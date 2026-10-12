## Entry 1152 — 2026-08-03 14:22 PDT — §5b step 2: the Annex A.1 documentation register, audited — and the step's own premise refuted

§5b ranked the 222 Annex-A.1 DOC rows as "the cheapest closure in the whole inventory", on the reasoning that
`docs/CONFORMANCE.md` "already carries a large numbered set, so an unknown but likely LARGE fraction of these
are already satisfied and merely unrecorded." That reasoning is now measured, and it is wrong.

**THE MEASUREMENT: 10 of 199 documentation obligations are discharged. 189 remain.**
`python scripts/spec/audit_annex_a1.py` computes it from `spec-rule-catalog.json`'s DOC rows, which
`extract_rule_catalog.py` already parses straight out of Annex A.1 — so this adds no second copy of the
register. The "large numbered set" §5b was thinking of is CONFORMANCE.md **§2 (Annex A.3**, processor-dependent
elements) and **§3 (Annex E** behaviour determinations). Those are *different registers*. §7 is the A.1
register, and it held ten rows.

⚠ **AND A CLAUSE-OVERLAP SEARCH FLATTERS BADLY — do not use one as a coverage proxy.** 61 of the 222 items have
their cross-referenced clause cited *somewhere* in CONFORMANCE.md, which looks like 27% coverage. It is not:
A.1-1 through A.1-5 all cross-reference §14.9.1 and exactly one of them (item 2, the ACCEPT device) is actually
determined. The clause is the right key for *identity*; it is a terrible proxy for *coverage*.

**THREE DEFECTS IN THE REGISTER'S OWN BOOKKEEPING, all found by building the check rather than reading it.**

1. ⛔ **§7 filed the §15.3.3.2 fractional-seconds determination under item 87. Item 87 is `FORMATTED-CURRENT-DATE
   (accuracy of returned time)`** — a different obligation, still undocumented. The determination belongs to
   **item 202**, "Time formats and corresponding function values (maximum precision not less than nine
   fractional digits)", whose text matches it exactly (validated with `cite.py --check 15.3.3.2`). The number
   was INHERITED and never re-derived — CLAUDE.md rule 1's named failure mode — and §7's own preamble says a
   wrongly-documented determination is worse than an undocumented one. Corrected.
2. **The split "164 required · 26 conditionally required · 29 optional" sums to 219, not 222**, and sat in both
   plan §11 A11 and CONFORMANCE.md §7. Measured: **164 required · 30 optional · 27 conditionally required ·
   1 unclassified**. The unclassified one is item 176 (SET, NaN payload), which states its class in prose
   rather than the standard "This item is required." sentence — a one-item parser gap in
   `extract_rule_catalog.py`, not a transcription defect. Its documentation obligation WAS detected correctly.
3. **Two §7 rows carried no item number at all**, so they discharged nothing and nothing could check them. Both
   determine A.1-**92**, which is one of the 23 elements the standard explicitly says need *not* be documented —
   so they are VOLUNTARY, and they only partially address A.1-56 (DISPLAY data conversion), which stays open.
   Labelled as such rather than left to read as coverage.

**THE DURABLE PART IS THE GATE, NOT THE CORRECTIONS.** `scripts/spec/audit_annex_a1.py --check` re-derives every
item number in §7 against the register and reports the exact remaining set. `--self-test` proves all four checks
can fail; and the gate was additionally run against a *re-introduced* item-87 mis-numbering and returned rc=1
with the right message, which is the only evidence that matters (`feedback_green_gates_arent_evidence`).

⚠ **MY OWN CHECKER WAS WRONG TWICE BEFORE THE DATA WAS**, which is becoming this project's most reliable
pattern. First it flagged 7 items as missing their clause cross-reference — six were a trailing `.` after the
`)` defeating my regex, and the seventh (A.1-20) has its cross-reference followed by a NOTE. All 222 have one.
Then it rejected a *correct* row, because I compared PROSE: A.1's preamble states outright that its headers are
"a paraphrase of the normative detailed specification", so demanding lexical overlap with a paraphrase demands
the wrong thing. **The cited CLAUSE is the identity.** It also failed a legitimate second determination for one
item as a "duplicate" — one A.1 element can need several rows.

**WHAT THIS CHANGES FOR THE CAMPAIGN.** §5b's vein ranking put DOC first for closure-per-effort. That ranking
rested on the refuted premise, and §5b + §11 A11 are corrected in this change set. The 189 remaining are
determinations to be MADE — read the implementation, settle the behaviour, write it, pin it so doc and code
cannot drift — i.e. 189 small design decisions, not a recording exercise. DOC keeps exactly one real advantage
over the GR/SR veins: it needs no compiler change *where the behaviour is already settled*. The veins should be
re-ranked against the measurement before the next campaign phase is planned, which is now recorded in §5b.

