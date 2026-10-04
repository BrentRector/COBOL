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
    <h3>In flight — Sunday 2026-10-04, afternoon</h3>
    <p><strong>No wave is running.</strong> Five trains have landed since the weekly reset, each CI-proven: <span class="pill good">1011</span> STRING and UNSTRING operand screens, the OO data-division placement table and constant-name integer positions (GAP 497 to 426); <span class="pill good">1012</span> linkage rules for function formals, SORT and MERGE record-size screens, the FLAG detectors and the binder driver (426 to 381); <span class="pill good">1013</span> ANY LENGTH RETURNING, interface covariance, variable-length group display (381 to 359); <span class="pill good">1014</span> extended letters in COBOL words with one Annex C fold, the parser suffix-order and SUPER grammar, the OO binder's receiver rules (359 to 336); <span class="pill good">1015</span> the ADDRESS OF operand rules, file-sharing defaults and fatal I-O status with the CI guard's NIST leg now run in every local gate, the OO class table, and the parser's second grammar cluster (336 to 289). Group Y (the OCCURS KEY, DEPENDING ON and OCCURS format rules) landed alone afterwards (289 to 281), with the fix for the one defect the train's review had found in it.</p>
    <p><strong>Waiting:</strong> PB1042 (a dynamic-capacity table in an EXTERNAL record) stays open as its own redesign slice; the lander's review rule now fixes a small confirmed finding in the train instead of dropping its cluster.</p>
    <p><strong>Tooling moved today:</strong> the local WSL distro and the CI runners are both Ubuntu 26.04; the CI guard job's NIST loop, which only CI used to run, is now part of the local gate; ANTLR reads grammars as UTF-8. PB1957 lists every other place CI and the local gates differ. An orchestrator loop prototype (a deterministic wave planner, an id allocator, a closed-rows ratchet and a supervisor that starts a fresh session per unit of work) is being built for testing: PB1981.</p>
    <p><strong>Partly done, by design:</strong> PB1042 and the ODO notes, the PB1425 SELF and SUPER identifier positions, the PB1136 receiver half, the PB244 residues, PB1722 and the OO universal-descriptor leg (PB480, PB1112).</p>
    <p><strong>Pacing:</strong> one seventh of the weekly quota per day is the target, with the owner's okay to borrow from the next day; models are sized per group.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured today</h3>
      <p>Five trains and one single-cluster landing moved 216 rows (GAP 497 to 281). Roughly 92 % of the estimated spend is in the agents and 8 % in the orchestrator session, and Opus agents are about a third of agent spend, so model routing and turns per agent are the levers.</p>
    </div>
    <div class="card">
      <h3>Owner decisions</h3>
      <p>Finish pending work first; new work inside the day's share of the quota, with borrowing only when allowed; the 5-hour session meter stays below its soft line; CI and the local gates should be the same build; Fable only with explicit approval.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
