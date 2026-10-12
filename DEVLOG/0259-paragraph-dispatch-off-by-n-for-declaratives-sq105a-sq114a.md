## Entry 259 — Paragraph-dispatch off-by-N for DECLARATIVES → SQ105A/SQ114A hang fixed; SQ213A un-vacuumed

**Entry 258's root-cause hypothesis was wrong (transparency).** I had blamed a "PERFORM … THRU return
defect when an internal GO TO redirects within the range." That was a misread of an indirect trace.
Per the standing instruction, I instrumented the **main** paragraph-dispatch loop only — logging every
`from->ret` transition plus an index→name map — and the actual defect fell out immediately.

**The bug.** Each paragraph compiles to a method returning the next `pc`. Every `pc` value — fall-through
(`myIndex+1`), GO TO (`ParagraphIndices[target]`), PERFORM THRU range bounds, GO TO DEPENDING targets —
lives in **paragraph-index space**, which is assigned over *all* paragraphs **including DECLARATIVES**
(`CreateParagraphStubs`). But the main dispatch switch indexed `ParagraphDispatchOrder`, which **excluded**
declaratives (`if (para.IsDeclarative) continue;`). For any program with leading DECLARATIVES paragraphs,
`switch(pc)` therefore resolved to the wrong method — off by the number of declaratives. With 2 declaratives
the trace showed the signature `+3` stepping (`dispatch[k]` = paragraph `k+2`, whose fall-through returns
`k+3`): the program walked CCVS utility paragraphs forever and never reached `STOP RUN`. This is why only
programs *with* declaratives hung; declarative-free programs were unaffected (so most baselines passed).
Ground truth: `DBG_IDX` showed SQ105A skips 2 declaratives → `dispatch index = ParIdx − 2`, while SQ202A
(no flagged declaratives) had `dispatch index == ParIdx` and worked.

**The fix (one consistent index space).** Include declaratives in `ParagraphDispatchOrder` so list position
== paragraph index (the `pc` value), and start the main loop at `EntryParagraphIndex` — the first
non-declarative paragraph (ISO §14.4: execution begins after END DECLARATIVES). Declaratives stay in the
switch at their own indices but are unreachable by the main loop: they are entered only via the USE
handler's `IrPerform` / `IrPerformThru` (direct calls / a self-contained ParIdx-space loop), never this
switch. Programs without declaratives get `EntryParagraphIndex = 0` and an unchanged dispatch list, so they
are byte-identical. (Confirmed: PERFORM single, PERFORM THRU, and GO TO DEPENDING already operate entirely
in ParIdx space and were never affected — only the main dispatch was.)

**Results.** Full guard ALL GREEN — 1000 unit / 348 integration / **213 NIST** (+SQ105A), **0 regressions**.
- **SQ105A**: infinite loop → **22/22**, baselined.
- **SQ213A**: its prior baseline was a **vacuous false-pass** (`000 OF 000` — the off-by-N dispatch sent it
  straight to termination, executing zero tests). Now genuinely runs **7/7**, including the `USE PROCEDURE`
  declarative tests. Baseline regenerated.
- **SQ114A**: hang gone (15/15 when dispatch resolves its paragraphs correctly) but **not yet baselined** —
  see the dup-name note below.
- **SQ121A**: hang gone; now exposes a *separate* REWRITE record-count bug (550 records) — not baselined.

**Two further dispatch bugs surfaced and were deliberately left for a separate, isolated change** (this
commit fixes only the well-understood declarative off-by-N; bundling would risk a regression and conflate
three root causes):
1. **Duplicate paragraph names across sections.** `ParagraphDispatchOrder` is built by **name** lookup
   (last-dup wins), and GO TO target resolution is likewise **name**-based (`ParagraphIndices[name]`), while
   fall-through is **symbol**-based. SQ114A has duplicate names and hangs under name-based dispatch but runs
   15/15 under symbol-based dispatch. The proper fix is to make dispatch order **and** GO TO / GO TO
   DEPENDING resolve by the bound `ParagraphSymbol` (already carried on `BoundGoToStatement.Targets`,
   resolved with section-scope qualification in `BindGoTo`) — consistently symbol-based everywhere. A
   half-measure (symbol dispatch + name GO TO) breaks NC102A, so the two must move together.
2. **Inverted PERFORM … THRU range.** NC102A `PFM-TEST-F1-10` does `PERFORM PFM-G-F1-10 THRU PFM-B-F1-10`
   where the exit paragraph physically *precedes* the entry section (`EmitPerformThru` assumes
   `start ≤ end`). It currently fails "RETURN MECHANISM LOST". NC102A's standing 39/39 baseline is itself
   partly vacuous — name-based dispatch skips 4 tests including this one — so the *correct* NC102A is 43
   tests, and getting there requires both #1 and #2. Left untouched here (name-based dispatch preserved →
   NC102A byte-identical at 39/39).

