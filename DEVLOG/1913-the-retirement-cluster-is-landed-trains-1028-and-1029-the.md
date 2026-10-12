## Entry 1913 — 2026-10-07 04:50 PDT — The retirement cluster is landed (trains 1028 and 1029); the oracle's verdict on the two grammar trains; the census filing and the orchestrator loop start

**Train 1028 (Entry 1911, `fdf1ef1dc`)** landed PB2112 (`CobolSharp.sln` → `Cobol.Net.sln`, 228 files, no alias), PB2111 with
PB1918 (the legacy wording swept from docs, register, skills and comments; PB166 and PB717 retired as moot) and PB2114 (nine copies
of the sending operand are one `sendingOperand` rule, ADD/SUBTRACT/DIVIDE share `receiversOrSendingOperand`, four GIVING phrases are
one; 20,494 lexer inputs token-identical, zero parse-tree differences; the additive-only caution deleted from §0). Its lander fixed
three findings in the train, among them a wrong DOC_INDEX sentence about what builds at the archive tag. Group C's launch had failed
with `error: parallel checkout finished with pending entries` while this session committed in the main checkout (Entry 1908's
commit, 02:51) — the rule is now a memory note: a Workflow launch is its own turn. **Train 1029 (Entry 1912, `f9bcff6d4`)** landed
PB2113 and PB1968: D10's first half, the SUBSCRIPT lexer mode, the `SUB_*` stream and the hand-written subscript parsers gone,
subscripts, reference modifiers and keyword-omitted argument lists parsed by the grammar, §9.4 settled from ISO §8.3.5. The
implementer SPLIT at the cap with two grammar reds the lander fixed in the train (`functionCall`'s argument alternative and
`refModSpec`'s operand). PB2151 files the second half (positions onto `BoundExpr`). Waves 1028 and 1029 cost 1,290,565 and
809,507 subagent tokens.

**The oracle, run on main after both trains** (`scripts/arch/compare_oracle.py` against the f620ccf6d43d manifest, 7,357 cases):
`=== ARCH-ORACLE: DIFFERENT — 39 of 7361 cases differ (baseline f620ccf6d43d, new f9bcff6d46e3) ===` — the emitted C# of 35
NIST programs and the diagnostics of four negative fixtures (`l1c08-entry-name-subscripted`, `l1c17-paren-not-delimiting`,
`pb1372-cobol-words-undefined-picture-syntax-withdrawn`, `pb1455-subscript-before-qualifier`), with every gate green on both
trains. That is the class the oracle exists for: program output is unchanged, the code's shape and four diagnostics are not, and
the negative fixtures' assertions are coarser than the oracle's hash. Filed as **PB2152** (cluster `PB2108`): capture both
sides, classify the 39 by mechanism, file any diagnostic that got worse as a defect, re-record the baseline. The process gap is
closed in this commit: the lander brief's step 3c and MANDATORY-PRACTICES L11 make every lander run `compare_oracle.py` after
its gate, explain each difference by class in the train's entry and re-record the baseline in the train commit;
`check_practices.py` pins the step.

**Cleanup and next.** `prune_worktrees.py --apply` archived ten unmerged tips and removed five landed worktrees; the dirty, recent,
CHECK and UNLANDED ones stay (two of wave 1026's read CHECK/UNLANDED because the lander re-recorded the oracle manifest under a new
name — nothing is lost, the content is on main). The meter read session 25 %, weekly 65 %, Fable 34 % at 04:30 PDT. Next, in this
order: a Sonnet clerk files the census findings as the Delete program's notes (cluster `PB2119`: dead artifacts, unreferenced-member
families per file, clone families, folder/namespace disagreements; god classes wait for R1 and test-only families need a judgment),
through a generator that the next census run reuses; then `orchestrate.ps1 -BorrowDays 3` runs the fix lane until the 85 % cap.

