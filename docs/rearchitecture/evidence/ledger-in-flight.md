<!--
  THE ONE HAND-WRITTEN SECTION OF THE CONFORMANCE LEDGER.

  `scripts/spec/gen_ledger.py` inserts this file VERBATIM as the body of the artifact's
  "In flight right now" section (inside <section aria-label="In flight">, after its <h2>), so it is an HTML
  fragment in a .md file rather than markdown — verbatim means verbatim, and a renderer standing between the
  author and the page would be a second author of the only part a person writes.

  ⛔ NO COUNT THAT THE GENERATOR CAN MEASURE BELONGS HERE. Row totals, verdict histograms, register standing,
  A.1 coverage, gate results and the corpus are all computed from the tree; writing one of them by hand here is
  how this page would start lying again. What belongs here is NARRATIVE: which lanes are running, what is
  queued behind the lander, which owner questions are open. Numbers are allowed only when they describe work
  that has NOT landed yet — a queued landing's row count exists nowhere the generator can read.

  ⛔ AND IT IS NOT A WORK LIST (CLAUDE.md rule 8). The register is `kb/Work/`. Anything here that starts to
  look like a checklist of remaining items is a note that should have been filed instead.

  Classes available: .flight (the accent panel), .cardgrid + .card, .pill.good/.warn/.crit, .mono, .num, .dim.
-->
  <div class="flight">
    <h3>In flight — Wednesday 2026-10-07, morning</h3>
    <p><strong>The legacy byte engine is gone from <span class="mono">main</span>.</strong> The retirement the owner decided on Monday night (<span class="mono">kb/Work R69</span>) has landed except for two pieces, both running in the orchestrator loop's campaign lane: the second half of the D10 ruling, which moves subscript and reference-modification positions off a bind-time C# string and onto the bound tree (<span class="mono">PB2151</span>), and the behavior-neutrality oracle's 39 differences from the two trains that carried D10 and the grammar unification, each to be explained or fixed before the baseline is trusted (<span class="mono">PB2152</span>). The tag <span class="mono">legacy-byte-engine-final</span> keeps the engine's last commit.</p>
    <p><strong>The architecture review's instruments are in.</strong> R0's census, oracle and performance baseline landed, and the census's findings were filed as the review program's waves; the section above reads their standing straight off the register.</p>
    <p><strong>R1, the target architecture, is in its third draft.</strong> Mythos 5.1 wrote it on the owner's approval for that one dispatch. An Opus refuter broke it twice and it was revised twice; Draft 3 is being written now. The owner took one decision on it this morning: the namespace roots become the project names (<span class="mono">Cobol.Net.Compiler</span>, <span class="mono">Cobol.Net.Frontend</span>, <span class="mono">Cobol.Net.Editions</span>, <span class="mono">Cobol.Net.Cli</span>), reversing PROJECT_ORG §1.1.</p>
    <p><strong>Running now:</strong> the orchestrator loop runs the fix lane, train after train, up to the owner's 85 % weekly cap and never past the session window.</p>
    <p><strong>Queued:</strong> R1 lands once the owner approves Draft 3, and the Delete program's first waves follow it.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions this week</h3>
      <p>Delete the legacy engine now; split the review's start; dead code goes; Mythos follows the Fable rule; every model is dispatched at its latest version; run all work to 85 % of the week, then pause; never exceed the session window; remove a worktree only after its work is committed and landed.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
