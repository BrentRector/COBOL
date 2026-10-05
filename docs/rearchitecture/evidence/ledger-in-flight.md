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
    <h3>In flight — Sunday 2026-10-04, evening</h3>
    <p><strong>No wave is running.</strong> The first wave driven by the orchestrator loop (wave 1018, a 2.5-hour unit) landed all eight groups in two CI-proven trains: <span class="pill good">1018</span> line-sequential WRITE keying, the OCCURS level, bound and CAPACITY rules, directive syntax in omitted conditional-compilation branches, INSPECT sign and item identification, and the SORT terminator, literal and object references; <span class="pill good">1018b</span> binary32 ACCEPT rounding, STRING receivers for every legal form, and CONTINUE AFTER saturation. Wave 1017, which died when its one-shot session reached the 600-second background ceiling, was recovered from its six checkpointed worktrees, and none of its work was lost.</p>
    <p><strong>Waiting:</strong> PB1425 stays half (the identifier-position work), PB1042 stays its own redesign slice, and PB2003 and PB2004 are new from train 1018.</p>
    <p><strong>Tooling landed today:</strong> the orchestrator prototype (PB1981) is on main and has now run real units: a quota meter, a resume and two waves. Defects the first real runs exposed are fixed: the supervisor owns each unit's lifetime through an open stdin, STOP winds a running unit down without losing work (<span class="mono">stop.ps1</span>), and frequent handoffs (PB2015) checkpoint a running unit and write the handoff a dead unit never did. The ledger is published after every landing.</p>
    <p><strong>Partly done, by design:</strong> PB1042 and the ODO notes, the PB1425 SELF and SUPER identifier positions, the PB1136 receiver half, the PB244 residues, PB1722 and the OO universal-descriptor leg (PB480, PB1112).</p>
    <p><strong>Pacing:</strong> one seventh of the weekly quota per day is the target, with the owner's okay to borrow from the next day; today stayed inside the borrowed allowance. Models are sized per group.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured today</h3>
      <p>Six trains and one single-cluster landing moved 242 rows (GAP 497 to 255) since the weekly reset. The model routing and turns per agent are the cost levers: most of the spend is in the agents, and the orchestrator session is a small share of it.</p>
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
