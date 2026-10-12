## Entry 1719 — 2026-09-26 16:07 PDT — Wave-61 train 62: file-control screens, literal screen, function and INVOKE arguments

Train 62 carried four fix-lane clusters in one landing, in manifest order. Three were planned: w61i, w61g and w61h. The
fourth, w61j (PB1137), finished while the first gate was running. Its branch already contained w61h, so it came
after cluster 3. The train was deliberately below the 4–6 band so it would land before the weekly quota reset.

**w61i — PB1072 + PB1074 + PB1080: the file-control clause operands pass their own §12.4.5 syntax-rule screens.**
This finisher merged the w61c predecessor onto train 61, re-ran every predecessor probe and got identical output. It
changed no code. SUPPRESS WHEN literal-1 is now read once, through the data division's literal-position chokepoint
(`DataBinder.RawValueOperandText`, `LiteralPosition.SuppressWhen`). The grammar operand became `valueClauseOperand`,
so constant-names, symbolic-characters and figuratives are decoded rather than refused at parse time. The operand is
screened by §12.4.5.6.3 SR7 (COBOLNET0863), and the §12.4.5.6.4 GR6 suppression value is sized to the key.
`SUPPRESS WHEN "XX"` on a PIC X(3) key now suppresses. File COLLATING SEQUENCE resolves by KEY CLASS (§12.4.5.7.4
GR2/GR3), with SR1/SR2/SR7 screened as written through one class test. A literal-defined national alphabet on a
national key is declined by name (COBOLNET1584; Annex A.3 item 41 is processor-dependent). FILE STATUS data-name-1 is
screened by §12.4.5.8.3 SR1–SR4 (COBOLNET2455) on a new model fact, `DataItem.Section` / `RootSection`.
Goldens: `2023/pb1072_suppress_when_forms`, `2002/pb1074_file_collating_key_class` and
`85/pb1080_file_status_item_forms`, plus five negatives. Rows: 9 + 7 (one DOCUMENTED-NON-SUPPORT) + 4.

**w61g — PB1393: a literal's own §8.3.3 syntax rules are asked once per literal TOKEN.** The new
`LiteralScreenPass` is a plain walk over every terminal of the raw tree, run from `BinderDriver`. It applies the
length rule (≤ 8,191 positions of the literal's class; §8.3.3.2.3 / §8.3.3.4.3 / §8.3.3.5.3 SR1; COBOLNET0814, now
catalogued as `LiteralTooLong`) and the hexadecimal grouping rule (§8.3.3.2.3 SR6 / §8.3.3.5.3 SR5; COBOLNET1635).
Before, each rule lived in one funnel. An over-long alphanumeric literal anywhere, and any over-long
VALUE/88/CONSTANT/ALL literal, compiled clean, as did `"Z" & X"4"` and the keyword-omitted `LENGTH(NX"041")`. A
`SUB_HEXLIT` lexer twin lets the screen see the keyword-omitted capture. The concatenated ALL literal-1 gains §8.8.3.2
SR2–SR4 through one statement, `ConcatFolder.ResultLengthViolation`. `LiteralScreenDriftTests` derives the screen's
token set from the lexer. The semgrep raw-diagnostic-code count fell 308 → 306 (baseline lowered). PB1406 was split
off to a finisher and is not in this train. Goldens: `2002/pb1393_hex_literal_positions` and the negative
`pb1393-concat-operand-hex-grouping`. 20 boundary cases are in `LiteralScreenTests`. Rows: 9.

**w61h — PB1418 + PB1115: a user-defined function's arguments go through the same §14.8.2 binder as a Format-2
CALL.** That binder, `ParameterConformance`, was extracted from `CallBinder`. It is imported by §8.4.3.2.3 SR13/SR14
(new twin codes COBOLNET2470/2471). The SR10 class screen asks the shared class answer. The GR5 manner asks the
receiving-operand prohibition table: `ExpressionBinder.ResolveReceiving`'s bars became
`ReceivingFormBar`/`ReceivingPlaceBar`, asked by both `ResolveReceiving` and `PermitsReceiving`. Three wrong answers
on the shared crossing were fixed. A string-valued intrinsic argument arrived as 0000. A figurative or ALL literal
BY CONTENT arrived as one character; it now fills an elementary formal, per §8.3.3.6.4 GR2. A nonnumeric literal into
a numeric formal was accepted; §14.8.2.3.3 2) a) now refuses it. Golden `2014/pb847_function_pointer_call` had passed
PIC 9 BY REFERENCE to a PIC S9(4) formal, which is non-conforming source. It was made conforming, and its output is
unchanged. Goldens: `2002/pb1418_function_argument_manner`, `pb1115_function_objref_conforming`,
`pb1418_call_content_figurative_fill`, plus eight negatives. Rows: 9 closed. PB1418 landed. PB1115 stays open for
SR-13.7.3-2 only (the prototype ↔ definition match; the proposed determination is in the note).

**w61j — PB1137: INVOKE's arguments ride the shared screens.** The implementer hit its turn cap without writing a
report. This paragraph is written from its STATUS, commits and note. `OoBinder` now asks `ParameterConformance` for
the storage-section rule (the new `SectionDataItem`), bit alignment and content conformance. That retires INVOKE's
third copy of the content rules, which w61h had reported as a lead. It decides the argument mode before resolution
(a bare property crosses BY CONTENT). It screens RETURNING SR11/SR12 and takes the universal ADDRESS-IDENTIFIER arm.
Literal-2 is bound through `ExpressionBinder.NonNumericLiteralOperand`: hex, national, zero-length (SR17,
COBOLNET2454) and NULL. The implementer's last code commit, `7fe2f0620` (BY CONTENT / keyword-less SELF as
identifier-5, with `SelfSenderRefusals` from SET Format 5), was not gated by the implementer; this train's
whole-assembly gate covers it. `ExpressionBinder.ResolveSending` resolves PAGE-COUNTER. New codes: COBOLNET2452–2454.
Goldens: `2002/pb1137_invoke_argument_forms` and four negatives. Rows: 6 CONFORMS, and 4 PARTIAL handed to
PB1051/PB1136.

**The train.** The clusters were brought in as patches and squash-merges. Inventory hunks were never merged: every
cluster's `record_verdicts` batches were re-applied in order on the merged tree. `docs/DIAGNOSTICS.md` and
`docs/DRIFT_RULES.md` were regenerated or unioned by whole rows (the only conflicts were whole-row). w61j's merge of an
earlier w61h was reconciled by taking w61j's side and re-applying w61h's two later commits (`5a827e145`,
`ea1bedb3c`). Gates on the whole `Cobol.Net.Tests.Conformance` assembly, unfiltered (filter `~CobolNet`), plus full
Unit and Characterization:
- Three clusters: Conformance 8985/8985, Unit 29448/29448, Characterization 33/33, GREEN.
- Four clusters: Conformance 8990/8990, Unit 29451/29451, Characterization 33/33, all passing. The verdict was RED on the citation audit alone: `kb/Work/PB1137.md` wrote "its SR17 half" next to SR-8.4.3.4.3-3, and `audit_code_citations.py` read that as §8.4.3.4.3, which has no SR17. The lander changed it to "§14.9.23.3 SR17". The three citation audits, `drift_rules.py --check` and `work.py check` were re-run clean on the final tree.
Legacy Integration: 503/503 passed (1 skipped). Semgrep: raw-diagnostic-code 308 → 301 (baseline lowered), every other count unchanged. Review: the lander's full-code pass over `git diff origin/main...HEAD` found no correctness defect and dropped no cluster. One observation: the INVOKE SELF refusal messages reuse SET Format 5's wording ("the method containing the SET statement"). **GAP 1109 → 1089 → 1080 → 1071 → 1065.** The
leads were filed as PB1615 (the COBOL-85 literal-length cap may be 160; analysis), PB1616 (directive-expression
literals are never screened; under-rejects) and PB1617 (a figurative BY CONTENT into a GROUP formal crosses as one
occurrence; wrong answer). DESIGN-version-conformance-pipeline §2.4.1 gains its missing `ClosedFormatPass` row.

