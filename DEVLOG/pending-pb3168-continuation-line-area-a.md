## PB3168: text of a fixed-form continuation line in area A is refused at --std 85 (COBOLNET3735) — the logical conversion asks, as a front-end error

**Re-probe (w1085f, base f5fec711b).** `E:/COBOL-coord/scratch/w1078f/probe/pc.cob` (a continued literal whose opening
quotation symbol stands in column 11) compiled at `--std 85`; so did a hyphen line holding only a period in column 10
(`pw.cob`). PB3147 had screened the first text of every non-continuation line, but a continued literal's tail begins no
token, so the area pass never saw the hyphen lines.

**Rule.** ISO 2023 has no areas (§6.3.1, §6.5 6) a) and b); `cite.py --check` OK). The repo holds no X3.23-1985 text; IBM
Enterprise COBOL for z/OS and OS/390 V3R2, "Continuation lines", states it outright: "Area A of a continuation line must be
blank" (`igy3lr10.pdf`, fetched and read). GnuCOBOL 3.2's `areacheck` does not ask it, so CLAUDE.md rule 1's precedence
reaches the IBM text; no implementor latitude is in play. Determination recorded in VCR row 7.35.

**Fix.** One rule, one row: the existing `fixed-form-text-in-area-a-2002` / COBOLNET3735, now with a second producer.
`LogicalLineBuilder.Add` asks `FixedFormLine.IsAreaAOffset(first)` of every fixed continuation line and reports
`ReferenceFormatDiagnostics.OnContinuationTextInAreaA` at the first text. `FixedFormLine` gained `ContinuationIndicator`
(the line classifier and the format detector read it) and `IsAreaAOffset` (`BeginsInAreaA` uses it). A period or word at
the start of a hyphen line is one diagnostic because the report is a front-end error: the compilation never reaches the
area pass (see the second review finding below).
The design alternative (a pass-side check) was rejected in the design doc's new paragraph: a resultant position has no
inverse to the physical line. The cost is named there: this is a front-end error, so it hides later bind-time gates of the
same program until fixed. A continuation line inside a comment-entry paragraph is commentary and not asked.

**Goldens.** `2002/pb3168_continuation_area_a` (quotation symbol in position 8, period in position 10: compiles and prints
the 54-character literal and 5), negative `pb3168-continuation-area-a-85` (COBOLNET3735 at (12,11)); unit cases in
`AreaPlacementPassTests` (positions 8, 9, 11 refused at 85 and legal at 2002; position 12 accepted; the period reported
once; library text). No existing test or NIST program holds a continuation line with text in area A (searched).

**Rows.** None: the inventory derives from the 2023 rules, which have no areas (`closes_rows: []` with its reason).
Codes COBOLNET3836–3838 were not needed. PB3147's residual paragraph now points here.

**Self-review.** Sibling arms: the area-B arm (PB3147) is the other producer of the same row and the two now agree on a
hyphen line (checked with the period case, one diagnostic). Free form has no areas and no hyphen indicator. The constant `'-'` was
spelled in the line classifier and is now `FixedFormLine.ContinuationIndicator`. Semgrep counts unchanged.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,627 · Unit 497 · Characterization 30 … Conformance 6,083/11,710 · Unit 33,176/33,673 · Characterization 6/36 cases ran (skipped 0) in 1 of 2 leg(s)` (run `20261015T205724Z-624491`).

**Review finding R1-1 (citation).** The branch review found that the unit test helper `FirstPartOfContinuedLiteral`
cited §6.3.5 2) for its continuation line; item 2) is the FLOATING indicator case, and the test continues the literal
with a FIXED hyphen, which is item 1). The doc comment now cites §6.3.5 1) for the fixed indicator and quotes the
paragraph common to both items for the continuation line ("the next line that is not a comment line or a blank line is
the continuation line", `cite.py --check 6.3.5` OK). The same misnumbering stands at about 40 older sites that cite
§6.3.5's unnumbered paragraphs as item 2), because `cite.py`'s printed rule path assigns a paragraph that follows the
list to its last item; that sweep is filed as a lead, not widened into this landing.

**Second review, finding R1-1 (dead mechanism, w1093c).** The branch review found that the plumbing meant to keep the
area pass from reporting a continuation line a second time could never run: `AreaPlacement.BeginsContinuationLineInAreaA`,
the map's per-line continuation flag, `FixedFormLine.IsContinuation` and the pass's exemption. `AreaPlacementPass` screens
only below 2002, where `fixed-form-text-in-area-a-2002` is not yet introduced, so `OnContinuationTextInAreaA` is an error
on both the strict and permissive axes (`EditionSeverityPolicy`). `CompilerDriver.Compile` then returns `FrontendError`
before the bind. The only continuation lines the builder does not ask (a directive's continuation, a comment-entry body,
a `*>` comment) carry no token. All four pieces are deleted (the map and the enum are main's again), and the design doc,
the pass's doc comment and the note now give the real reason. `ContinuationLineHoldingAPeriodInAreaA_IsReportedOnce` now
also asserts `Outcome.FrontendError`, so the once-only claim is pinned to the mechanism that provides it. Sibling found
while there: the reference-format detector spelled the hyphen indicator `'-'` a second time and now reads
`FixedFormLine.ContinuationIndicator`.
