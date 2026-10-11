## Entry 934 — 2026-07-20 13:10 PDT — RW SUPPRESS (§14.9.45) lands; the owner's "resolve from the SPEC, not the implementation" caught my inverted sum-reset research; and a Wave H fixture bug the local gate had missed

Grammar batch position 2. `SUPPRESS PRINTING` (§14.9.45) inhibits the printing of a report group. The RW engine
was already built for it — `ReportWriter.cs` carried a staged comment "the SUPPRESS statement … is not yet
parsed — its suppression flag is staged with it," and USE BEFORE REPORTING (§14.9.49 Format 2) already fires a
`BeforeReporting` hook just before each group is produced. So this was: a grammar rule (`suppressStatement :
SUPPRESS PRINTING`, wired into `statement`; both tokens already existed), a `BoundSuppress(ReportModel)` node,
`BindSuppress`, an emitter one-liner (`__RPT_n.SuppressPrinting()`), and the engine seam — plus one new
diagnostic, COBOLNET1581 (SR1).

**Where the target group comes from — resolved from the SPEC, not the code.** §14.9.45.4 GR1: SUPPRESS inhibits
printing only "for the report group named in **the USE procedure within which the SUPPRESS statement appears**,"
and SR1 restricts SUPPRESS to a USE BEFORE REPORTING procedure. So the target group is a STATIC, lexical
property — fixed at compile time by which declarative encloses the statement — which settles the
bind-vs-emit question the spec's way: WHICH group is bind-time (the enclosing declarative's group), WHETHER a
given instance is suppressed is runtime (GR2 — "limited to the current instance"). `BindSuppress` finds the
declarative whose pc range covers `BindCursor` (the declaratives already carry their `ReportGroup`), resolves
its owning report, and stores it; a SUPPRESS outside any such procedure has no group and is COBOLNET1581.

**The owner's process correction, and the bug it caught.** Mid-implementation the owner said: *"All questions
about appropriate behavior of some syntax should be resolved from the PDF specification instead of the, possibly
incorrect, implementation."* Applying that to the sum-counter interaction OVERTURNED my own prior research. My
pre-implementation note (and the compaction summary) claimed SUPPRESS should SKIP the end-of-group sum reset,
citing "§13.18.54.4 GR2/GR10." Reading the actual text: **GR10 scopes the "not reset" rule to PRESENT WHEN /
OCCURS DEPENDING absence — NOT SUPPRESS** (*"If the entry is associated with an absent data item as a result of
a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase, the corresponding sum counter is not printed
and is not reset to zero"*). And GR2 ties the reset to "the end of the **processing** of the report group,"
which §14.9.45.4 GR3 does not inhibit (GR3 inhibits only printing, page advance, NEXT GROUP, and LINE-COUNTER).
The spec even signals it by contrast: it knows how to say "not reset" (it does, for absence) and pointedly does
NOT for SUPPRESS. So the correct behavior is the OPPOSITE of my note — **a suppressed control footing STILL
resets its sums, and a suppressed detail STILL accumulates (GR7 happens in Generate, before the group's
presentation)**. Had I coded my original "skip the reset," a suppressed CF would have carried its subtotal into
the next control group — a silent wrong total. The engine seam reflects the corrected semantics: `PresentBody`
runs `EndOfGroupSumReset` on the suppressed path and only skips the PRINTING half; the heading/footing methods
(no sum reset) return after the hook. One shared `RunBeforeReporting` consumes the one-shot flag (GR2).

The golden `tests/conformance/2002/rw_suppress.cob` pins exactly this: three details generated into group 1 with
the MIDDLE one suppressed — its detail line never prints, yet the control footing prints `TOTAL=060` (= 10 + 20
+ 30), not 040. That single number is the whole proof; my original design would have printed 040. GreenfieldOnly
(legacy has no SUPPRESS grammar). Verified: greenfield corpus 296/296.

**A Wave H fixture bug the local gate had missed (committed separately, `ef080854`).** Running the corpus suite
on a FRESH build surfaced a `FormatException` in the negative-case harness: my Wave H `user-word-validate.cob`
wrote `reject-at: 2002,2014,2023` (commas), but the harness splits editions on SPACE and `int.Parse`s each
token. Every other negative fixture uses spaces; this one was the outlier. Wave H's gate had reported 3701/0
without catching it because the conformance assembly it ran was STALE relative to the just-added fixture (the
`[Theory]` MemberData did not include it) — the `feedback_fresh_build_before_no_build_test` hazard, live again.
Fixed to spaces; the negative case now rejects at all three editions with COBOLNET0901.

Ships as one change set: grammar + `BoundSuppress` + `BindSuppress` + COBOLNET1581 + emitter + the 4 engine
presentation methods + the `rw_suppress` golden + manifest + GreenfieldOnly + regenerated DIAGNOSTICS.md +
CONFORMANCE.md (A.4.11 row) + `COBOLNET_REPORT_WRITER_DESIGN.md` (verb table + §5) + plan §0 (diag band 1581
shipped → next-free 1582; grammar-batch worklist). Diag scans agree at 1581.

