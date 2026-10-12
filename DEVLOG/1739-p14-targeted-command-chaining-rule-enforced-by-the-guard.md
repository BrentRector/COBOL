## Entry 1739 — 2026-09-27 12:48 PDT — P14: targeted command-chaining rule, enforced by the guard hook; skills and doc index document the new economy

Owner, 2026-09-27: "If I recall, my daughter's Carly's skills request no use of chained commands. Evaluate whether we
should adopt something similar", then "Adopt your suggestions and update/use our skills in accordance", then "Update
the skills documentation appropriate to explain all of these changes, both to Claude reading the skills, and to
developers browsing them."
**The evaluation.** `carlymr/carlys-claude-skills` carly-code-review says to use Grep/Read rather than shelling out,
and "Do not chain commands with `&&` or `;`". Chaining has hurt here in one specific way: a chain's exit status is its
LAST command's, so `dotnet test … && git commit` committed on false greens. The gate skill forbade that, and it was
broken anyway. A blanket ban would turn each chain into extra turns, and agent cost is quadratic in turns
(`0.115·T + 0.00031·T²`). So the rule was adopted in a targeted form.
**MANDATORY-PRACTICES P14:**
- (a) Never chain anything after a verdict command (build, test, gate, push-main, battery); `; echo "EXIT=$?"`
  captures its status.
- (b) No `;` joining steps that must stop on failure, and no `cd` inside a chain.
- (c) Independent commands go as PARALLEL tool calls in ONE turn.
- (d) Short `&&` chains of read-only or fail-fast steps stay allowed.
**Enforcement:** `scripts/hooks/forbidden_commands.py` rule 5 blocks (a) on every Bash/PowerShell call, subagents
included. It ignores separators inside quotes, allows the `$?` capture and read-only reads after it, and allows
pipes/redirects on the verdict command itself (rule 4 requires them). Self-test: 35/35, including four blocked shapes
and six legitimate neighbours. (b) stays written practice, because `cd` chains are load-bearing in the push rule and
`;` in loops is legitimate.
**Documentation:**
- The gate skill's chaining rule now points at P14 and the hook.
- The workstream skill explains the orientation economy: `orient.py` in every dispatch spec, with the measured
  baseline (wave 65: 63 % of tool calls were reads/searches, 15 % of turns and 43 % of result bytes came before the
  first edit) and an estimate of about 15 % of implementer tokens saved, to be verified on wave 67.
- The workstream skill also explains P14 and why the blanket ban was rejected.
- `docs/DOC_INDEX.md` lists the hook's new rule and gains a row for `fix_clusters.py` and `orient.py`.
The implementer report template gains a required "For the next implementer" section: one `path#Member` line per changed file, naming the entry point, the helper to reuse and the trap. The same lines go into the note's Landing section, which is what `orient.py`'s LEARNED section reads, so each wave starts where the last one finished. The public skills repo (BrentRector/claude-skills v1.4.0, 25da43a) carries the generic versions: agent-fleet §1/§2, `references/orient.py`, the test-gate verdict rule, the guardrails rule `no-chain-after-verdict`, and eval `agent-fleet-orientation` (WITH 1.00 / W/OUT 0.00).

