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
    <h3>In flight — Wednesday 2026-10-07, before dawn</h3>
    <p><strong>The legacy byte engine is gone from <span class="mono">main</span>.</strong> On Monday night the owner decided the completion plan (<span class="mono">kb/Work R69</span>): delete the CobolSharp engine now rather than at v1.0, start the architecture review's measurement and design ahead of zero GAP, mandate the removal of dead and obsolete code, and keep the frontier model to the one design artifact that earns it. Trains <span class="pill good">1025</span> and <span class="pill good">1027</span> carried the two cuts: no test, script or CI job runs the engine, and then its five project trees and every reference to them. The annotated tag <span class="mono">legacy-byte-engine-final</span> marks the last commit that holds it; nothing in the build depends on it any more.</p>
    <p><strong>The review's instruments landed in train <span class="pill good">1026</span>:</strong> a Roslyn census of every type, clone family, unreachable member and dead artifact in the greenfield compiler; a behavior-neutrality oracle that hashes the emitted C# of every program the suites compile and the diagnostics of every negative fixture, so a restructuring wave can prove it changed nothing; a performance baseline measured against GnuCOBOL, which already shows where the typed-native storage model and the unindexed indexed-file connector cost; and a planner that can dispatch a named campaign cluster, not only harm-flagged defects.</p>
    <p><strong>Running now:</strong> the second retirement wave — the prose and comment sweep, the solution file's rename, the SUBSCRIPT lexer mode's removal in favour of interpreted grammar rules (the D10 ruling, blocked for months by the frozen oracle), and the unification of the grammar alternatives that oracle forced. The Sonnet and Opus groups are sized per item; one lander lands them as a train.</p>
    <p><strong>Waiting on the owner:</strong> the review's target architecture (R1) is to be authored by Mythos 5.1 and needs the owner's explicit approval for that one dispatch; PB643 (RESERVE areas at run time) still needs a reading of the unbuffered shared-writer posture.</p>
    <p><strong>Then:</strong> the orchestrator loop takes the fix lane and the external-repository slices, within the owner's pacing for this week — all lanes until the weekly meter reads 85 %, and never past the session window.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured this night</h3>
      <p>The retirement's two cuts and the four review instruments ran as four hand-dispatched waves beside each other; each landed green on Windows, Linux and CI. One launch collided with a commit in the main checkout and was re-dispatched alone, which is now a written rule.</p>
    </div>
    <div class="card">
      <h3>Owner decisions this week</h3>
      <p>Delete the legacy engine now; split the review's start; dead code goes; Mythos follows the Fable rule; run all work to 85 % of the week then pause; never exceed the session window; remove a worktree only after its work is committed and landed; a scoped rule lets an agent remove a tree inside its own worktree.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
