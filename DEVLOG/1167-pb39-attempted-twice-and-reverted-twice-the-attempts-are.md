## Entry 1167 — 2026-08-04 02:41 PDT — PB39 attempted twice and reverted twice; the attempts are what found the mechanism

**I tried to fix PB39 — the 326 catalog rule-ids that do not match the standard's own numbering — got it wrong
twice, and reverted both times. The value is in what the failures proved, so it goes here rather than being left
as a scar in the history.**

**ATTEMPT 1 — a parent STACK.** On an ordinal that exceeds the current sub-list's last but continues the PARENT's
run, pop back instead of extending. It measurably helped: **40 sub-lists / 326 rules → 23 / 121**. Then it
**mis-split §15.68.3**, whose raw ordinal run is:

```
1\)  2\)  3\)  4\)  5\)      1.  2.  3.  4.  5.  6.  7.      6\)  7\)
```

Nine top-level rules with a **seven-item nested list** inside rule 5. **No arithmetic on the ordinals can tell
the nested `6.` from the top-level `6\)`** — the stack pops on the nested `6.` because it happens to equal
parent-last + 1. The heuristic is not merely incomplete; on this shape it is wrong.

**ATTEMPT 2 — the delimiter form**, which is the right idea implemented badly: a heredoc mangled the regex
replacement (Python printed `SyntaxWarning: invalid escape sequence` and I proceeded anyway), so the nested
`6.`/`7.` were promoted to top level and the real `6\)`/`7\)` demoted — the clause inverted. Reverted.

⭐ **THE MECHANISM, AND IT IS IN THE SOURCE RATHER THAN INFERABLE FROM THE NUMBERS.** §15.68.3 writes its
top-level rules `1\)` `2\)` … and its nested list `1.` `2.` … . The `ORDINAL` regex accepts BOTH forms — which is
precisely why they collide, and why every number-based rule fails on this clause.
⚠ **The form is per-BLOCK, not global.** The transcription uses `1.` as the TOP-LEVEL form in §8.8.3.2,
§11.9.11.2, §13.18.24.3 and §14.9.30.3 — the `ORDINAL` comment records that an earlier version matched only `1)`
and silently dropped every rule in those four clauses. So the rule is: *whatever form this block OPENED with is
its top level; a change of form opens a nest, a return to that form closes every open nest.*

**THE RAW RUNS, recorded so the next attempt is measured against all of them rather than against §9.3.8.2.3
alone** — that clause is the EASY one, which is how attempt 1 looked like a success:

| clause | raw ordinal run |
|---|---|
| §15.68.3 | `1,2,3,4,5, 1,2,3,4,5,6,7, 6,7` |
| §13.18.60.4 | `1…22, 1,2, 1,2, 23,24,25,26` |
| §12.3.7.4 | `1…7, 1,2, 2,3,4,5,6, 8…19` |
| §14.9.13.4 | `1,2,3,4, 4,5,6, 5` |
| §9.3.8.2.3 | `1,2,3,4,5, 1,2,3, 6,7,8,9` |

**STATE AFTER THE REVERT:** catalog byte-identical to its committed form, 3,981 rules, `--check` clean with zero
parse gaps, `SpecTraceabilityInventory` 10/10. PB39 stays OPEN with the mechanism and the migration note — still
only two adjudicated rows affected (`AR-15.68.3-L3.6` / `-L3.7`).

⚠ **AND THE PROCESS LESSON I SHOULD HAVE APPLIED TO MYSELF.** Attempt 2 printed
`SyntaxWarning: invalid escape sequence` *before* it ran, and I read the output instead of the warning. This
session has spent all day on the principle that a green result proves nothing about what the check actually
LOOKED AT — a warning I skip is that same failure one layer up, in my own tooling. On the third edit the warning
appeared again and I stopped to verify the rendered text before continuing, which is the behaviour that should
have been there the first time.

