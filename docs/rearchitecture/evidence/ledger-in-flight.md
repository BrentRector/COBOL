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
    <h3>In flight — Saturday 2026-09-26, afternoon</h3>
    <p><strong>Landed today.</strong> <span class="pill good">golden lane 2</span> witnessed 59 rows that were already correct but owed a spec-derived test. <span class="pill good">train 61</span> landed five groups of wrong-answer fixes in one CI-proven landing: COPY/REPLACE text-words, runtime I-O boundaries with owner decision R47 (Latin-1 files refuse characters above U+00FF), one PICTURE-clause identity for OO and CALL conformance, record sizing from the RECORD clause in bytes, and floating-point ROUNDED MODE and size-error handling.</p>
    <p><strong>Still implementing &mdash; wave 61</strong>: literal syntax screens and user-defined-function argument binding are running; the file-control clause screens are complete and await their final gate; INVOKE's argument binder (PB1137) resumes Sunday. These ride train 62.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions today</h3>
      <p>R50: user documentation is docs-as-code published with Astro Starlight on wiseowlsoftware.com, distributed on NuGet under <span class="mono">WiseOwl.</span>. Contributions are accepted under a CLA; the GitHub community standards are in place.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
