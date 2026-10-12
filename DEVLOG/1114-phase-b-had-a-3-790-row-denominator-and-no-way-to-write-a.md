## Entry 1114 - 2026-07-29 19:20 PDT - Phase B had a 3,790-row denominator and no way to write a verdict into it

**THE STATE I FOUND.** Phase A of the spec-conformance review is complete: `spec-rule-catalog.json` holds 3,790
normative rules with their full text, `build_inventory.py` seeds one inventory row per rule, and
`session-probe.ps1` reports `3790 rows · 3790 GAP`. That number is the definition of v1.0 (D13: zero GAP = P14
done). It has never moved, and the reason is not that the work is hard. **There was no mechanism to record a
verdict.** `build_inventory.py` only ever PRESERVES the adjudicated fields across a rebuild; nothing sets them.
The only way to record one was to hand-edit a 1 MB JSON array — which loses a batch the first time two writers
overlap, and offers no point at which anything is checked.

That second half is the real problem. **The GAP count is a completion metric, so every row is a claim about
completion, and an unaudited claim about completion is a self-report.** Closing a row by hand cost one string.

**WHAT LANDED — four parts, and the boundary between them is the design.**

| part | owns |
|---|---|
| `tests/version-matrix/inventory-schema.json` | the RULES, as data: the six verdicts, each one's `resolves` flag and required evidence, the legal editions, the `code-location` pattern, the `test-ref` forms |
| `scripts/spec/inventory_schema.py` | parsing that schema, deriving `state`, the ATOMIC write |
| `scripts/spec/record_verdicts.py` | validating a batch's SHAPE, merging all-or-nothing |
| `SpecTraceabilityInventoryDriftTests` | REFERENTIAL integrity, continuously, in the battery |

**The vocabulary is data because three engines read it.** The row builder, the writer and the C# gate all need the
verdict list and the required-evidence matrix; before this, `build_inventory.py` carried its own `RESOLVED` set
and its docstring carried a second copy of the vocabulary in prose. Both are gone. Adding a verdict is now an edit
to one JSON file. Having just spent a wave on DA3 (three copies of one dispatch) and DA5 (two predicates for one
question), writing the list down a fourth time was not available.

**SHAPE AT RECORD TIME, REFERENCE AT BATTERY TIME — deliberately with no overlap.** This was the one genuinely
hard call. Both the writer and the gate want to ask "does this reference resolve?", and implementing it twice is
the anti-pattern. So they split by WHEN the question has to be answered, not by convenience: the writer checks
what is knowable about a batch in isolation (rule-id real, verdict in vocabulary, required fields filled, editions
legal, each reference parses) and stops. Whether a symbol still exists and a test is still on disk has to keep
holding as the tree changes underneath a row recorded sessions ago, so it belongs to something that runs every
build. Neither side hard-codes a verdict name or a form — both read the JSON — so what exists twice is a ten-line
evaluator, not the rule. And the divergence that would matter is itself gated: the C# side recomputes every row's
`state` and fails if it disagrees with the stored one, so a Python evaluator drifting from the C# one turns the
battery red rather than quietly inflating the burn-down.

**⛔ A DESIGN CORRECTION, made and recorded in the same change set.** `DESIGN-spec-conformance-review.md` §4
specified `code-location` as `file:line`. That is wrong at this scale and I changed it to `path` or `path#Symbol`.
A line number rots on the next edit to the file; 3,790 rows of them would hold the gate permanently red for
reasons having nothing to do with conformance, and **a gate that cries wolf is a gate nobody reads** — which
would have cost more than the traceability it bought. A symbol survives refactoring, moves with the code it
names, and fails only when the thing it points at actually stops existing. The doc now carries the correction and
the reasoning, and the writer REJECTS a line-number form outright (verified: `a.cs:123` fails validation).

**ALL-OR-NOTHING, THEN AN ATOMIC REPLACE.** Every record across every named batch validates before any of them
merges, and the write goes through a temp file plus an `os.replace`. A half-applied batch is the worst outcome
available here: the rows that DID land look exactly like reviewed work and nothing afterwards can distinguish
them. A torn write is worse still, because a truncated JSON array is not a parse error — it is a valid, silently
shorter inventory.

**⛔ THE GATE IS PROVEN ABLE TO FAIL, TWICE OVER.** A gate observed only passing is indistinguishable from one
that inspects nothing (`feedback_green_gates_arent_evidence`), and this one will spend most of its life green over
rows nobody has touched.

1. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives all nine checks against rows built to break them —
   one defect class at a time — **plus positive controls**, so a failure cannot come from a checker that simply
   rejects everything. It runs on every build, so the evidence does not decay.
2. And once against the REAL artifact: I corrupted `traceability-inventory.json` in place — one row hand-promoted
   to `OK`, one CONFORMS row citing a file and a golden that do not exist — and got exactly three reds
   (`EveryRowState_IsDerived_NotAsserted`, `EveryCodeLocation_ResolvesInTheTree`,
   `EveryTestRef_ResolvesToARealTest`), then green again on restore. That is the wiring test the synthetic rows
   cannot give: it proves the checks are pointed at the real file, the real schema and the real catalog.

The writer's validator was exercised the same way, on a batch carrying seven deliberate defects — unknown
rule-id, unknown verdict, missing required evidence, unknown test-ref form, a duplicate rule-id within the run, an
illegal edition, a line-number `code-location`, an unknown field. It reported all 14 violations at once and wrote
nothing. Reporting them together rather than one at a time is deliberate: a batch produced by a fan-out of agents
tends to carry the SAME mistake in many rows, and seeing all of them turns a twenty-round correction loop into one.

**WHAT IS DELIBERATELY STILL A GAP.** `NEEDS-OWNER-DECISION` does not resolve a row. An unanswered question is not
coverage, and the metric must never be cheaper to move than the work it stands for. `DOCUMENTED-NON-SUPPORT` does
resolve one, but it is an owner decision (D13) and agents are forbidden from choosing it.

**GATES.** Solution build clean. Unit **953/953** (942 + 11 new). Characterization **33/33**.
`build_inventory.py --stats` unchanged at 3790/3790 after the refactor.

