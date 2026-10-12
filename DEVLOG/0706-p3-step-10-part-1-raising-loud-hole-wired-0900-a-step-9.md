## Entry 706 — 2026-07-08 18:52 PDT — P3 Step 10 (part 1) — RAISING loud-hole wired (0900); + a Step-9 legacy hotfix

**PHASE 03 step 10 (full wiring, owner-chosen) — first loud-hole fix.** The sweep found the one clear generic-hole:
`PROCEDURE DIVISION … RAISING` below 2002 emitted a GENERIC `COBOL0001` ("no viable alternative at 'RAISING'")
instead of a specific edition diagnostic. Wired it:
- `ReservedWordEditionHints`: a new arm `CobolLexer.RAISING when InRule(ruleStack, "procedureDivision")` →
  `Constructs.ProcedureRaising2002`, so `CobolErrorStrategy` re-diagnoses it as **`COBOLNET0900`** (verified: RAISING@85
  now emits `COBOL0001` + `COBOLNET0900`, exactly like the XOR/SHARING residue). **R1 check:** `RAISING` is a
  multi-construct token, but `GOBACK … RAISING`@85 does NOT mis-fire this arm — it PARSES and bind-gates
  (`COBOLNET0879`), so it never reaches a parse-error-at-RAISING; the header raisingClause (parse-gated
  `{is2002()}?`) is the only usage that produces the parse error the arm keys off. The arm carries a rule guard
  (more specific than the bare SHARING/RETRY/UNLOCK arms). Guard confirmed no legacy regression.
- Flipped `procedure-raising-2002` from `pending` → `active` (a valid positive `PROCEDURE DIVISION RAISING
  EC-USER-MYERR.` compiles at 2002+; below 2002 rejects with 0900). Added a negative corpus fixture
  `raising_below_2002` (`*> reject-at: 85`, `.err` = COBOLNET0900) — the discovery runner auto-tests it.

**Step-9 legacy hotfix (caught by THIS step's guard run):** my Step-9 seed `arithmetic_standard_decimal.cob` is a
shared-corpus case the LEGACY `ConformanceTests` also discovers, and the frozen legacy can't bind ARITHMETIC IS
STANDARD-DECIMAL → 1 red integration test. Added `("2014", "arithmetic_standard_decimal")` to the legacy
`GreenfieldOnly` skip-set (same as the existing `options_paragraph`). ⚠ Lesson reinforced
([[feedback_legacy_suite_on_shared_corpus]]): a shared-corpus addition needs the LEGACY conformance suite (the full
guard) IN THE SAME commit — I skipped the guard on Step 9 ("corpus data only") and it bit here.

**Battery:** greenfield conformance **3103** · unit 227 · characterization 32 GREEN; FULL legacy guard NIST **353
MATCH**, legacy unit 1196, integration **608 GREEN**. Step 10 continues: SYNCHRONIZED-on-group (a 2023 feature
silently ACCEPTED below 2023 — a disposition decision) + NO SIGN / inline-method (un-gatable-cleanly residue).

