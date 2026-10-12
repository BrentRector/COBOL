## Entry 1253 — 2026-08-08 22:55 PDT — PB29's campaign completes: all 100 unharvested clauses adjudicated, the denominator grows to 4,311, and the blind-spot class closes guarded

The second catalog campaign is done, and with it the register's owner-directed handoff. The remaining
85 clauses of the owner-authorized batch ("adjudicate all 100, admit the normative ones") were
adjudicated in one session: 81 admitted (+330 rules), 4 not-rules, 0 unsure, 0 pending. The
denominator is 4,311 (was 3,981), GAP 4,233 (was 3,903) — the GAP going UP is the model working:
adjudication opens work; only Phase C closes it. §11 A3 now audits the intermediate-results model
against a catalog that carries all of §8.8.1.

The method was the campaign's own doctrine: seven reader agents (each with its own input file) over
the 84 single-list clauses, then two adversarial refuters re-reading every clause body under orders
to overturn. 83 of 84 verdicts survived; the one overturn — §15.6 rules→not-rules, its four items
being reading conventions for Table 21 that no processor can obey or violate — is a DOWNGRADE, which
keeps this campaign's record intact: every refuter overturn ever recorded is one. I adjudicated
§8.3.2.2 myself and re-verified every not-rules verdict and the overturn against the clause text
before applying anything. The four exclusions each carry their grounds in the manifest: §9.1.1
(illustrative "include such features as"), §10.3 (bare division names — the order constraint is
normatively carried by the §10.6.1 general formats, which are already FMT rows; the refuter supplied
that citation, upgrading the reader's thinner reason), §15.1 (describes the §15 subclauses
themselves), §15.6 (the overturn).

The mechanism work DEVLOG 1166 predicted arrived on schedule, twice. §8.3.2.2 is admitted with
PER-SUBLIST kinds — kind: {"sublists": {"1":"SR","2":"GR","3":"SR","4":"GR","5":"GR"}} — and the
batch immediately surfaced the sibling shape: SEVEN clauses are mixed per-ITEM (§5.5, §6.1,
§8.3.3.3.2, §8.3.3.3.3, §8.5.1.9.1, §8.8.2, §14.7.7 — e.g. §8.3.3.3.2's items 1–3 constrain the
literal's written form, SR, while item 4 states its value, GR), expressible as kind: {"items":
{...}}. Both shapes resolve at flush; a sub-list or item the map does not know is FATAL, proven by
perturbation before being trusted (dropping sub-list 5's kind flipped the extractor to exit 1).

Every count delta was attributed before landing: the harvest added 315 rows where the manifest
arithmetic said 314, and the +1 is GR-8.4.6.2.1-L2.2 — a page-break-flattened nested tail (item 1
indented, item 2 at column 0), the exact PB55 shape, filed per the PB39-established treatment.

Gate: extractor --check exit 0 with zero parse gaps, zero unexplained ordinals, and the manifest
exact; build_inventory carried 421/421 verdicts and seeded 330 NEW GAP rows; work.py check clean
(112 items); SpecTraceabilityInventoryDriftTests 10/10 after the build guard's fleet window cleared.
The content-keyed scan now reports ZERO clauses awaiting a disposition — the third instance of the
denominator's oldest defect class (a rule-bearing clause whose heading names no rule kind) is not
just fixed but closed and guarded: a new such clause fails --check as manifest drift, and the
per-sublist/per-item FATAL keeps a segmentation shift from silently mis-typing what was adjudicated.

With PB29 and PB39 both landed, the owner's handoff is discharged. Open in the register: PB55 (the
denominator question the campaigns exposed — whether nested items are rows or rule text — an owner
decision), plus the A-series analyses and the P14 campaigns proper.

