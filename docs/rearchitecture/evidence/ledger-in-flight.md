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
    <h3>In flight — Monday 2026-10-05, morning</h3>
    <p><strong>No wave is running.</strong> Wave 1019, the second wave the orchestrator loop ran unattended, landed train <span class="pill good">1019</span>. Its five groups covered the TURN directive operands proven whole, the omitted leftmost reference-modification position, four PICTURE rules (SR14 bounds, basic letters, MESSAGE-TAG placement, the currency literal class), three formats now parsed as printed (UNSTRING INTO, LOCK ON, ALTERNATE RECORD KEY) with ASSIGN USING gated at 2002, and identifier Format 5, the object-view.</p>
    <p><strong>Queued for the next train:</strong> two finished, gated branches are held because two are fewer than a train's minimum of three. One is the directive-line separators and the COBOL-WORDS literal reader (PB1373, PB2003). The other is the BASED residence rule and four data-division notes (PB516, PB486, PB1650, PB1744), carrying PB1301, PB1302 and PB1476 from its predecessor.</p>
    <p><strong>Waiting:</strong> PB643 (RESERVE areas at run time) needs an owner reading of the unbuffered shared-writer posture; PB1525 (the nonstandard-extension register) and PB1425's remaining identifier positions are hand-offs; PB2023 and PB2024 are new.</p>
    <p><strong>Tooling:</strong> the stall watchdog reported every just-started agent as stalled, which ended its watch each time. It is fixed in the public skills (brent-tools 1.17.1, PB2033).</p>
    <p><strong>Partly done, by design:</strong> PB1042 and the ODO notes, the PB1425 SELF and SUPER identifier positions, the PB1136 receiver half, the PB244 residues, PB1722 and the OO universal-descriptor leg (PB480, PB1112).</p>
    <p><strong>Pacing:</strong> one seventh of the weekly quota per day is the target, with the owner's okay to borrow from the next day; today stayed inside the borrowed allowance. Models are sized per group.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured today</h3>
      <p>Seven trains and one single-cluster landing moved 265 rows (GAP 497 to 232) since the weekly reset. The model routing and turns per agent are the cost levers: most of the spend is in the agents, and the orchestrator session is a small share of it.</p>
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
