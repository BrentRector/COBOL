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
    <h3>In flight — Sunday 2026-10-04, morning</h3>
    <p><strong>Wave 1014 is running</strong> (three implementers, then a lander train), with the model sized per group: the extended-letters lexer work dropped from train 1007, with the ANTLR grammar-encoding fix (Sonnet), and the parser-core grammar and the OO binder (Opus).</p>
    <p><strong>Landed this morning</strong>, each CI-proven: <span class="pill good">1011</span> STRING and UNSTRING operand screens, the OO data-division placement table and method BY VALUE formals, the report-writer binder residues, constant-name integer positions and TYPEDEF RENAMES (GAP 497 to 426); <span class="pill good">1012</span> linkage rules for function formals, SORT and MERGE record-size screens, the FLAG-02 and FLAG-14 detectors, the binder driver (426 to 381); <span class="pill good">1013</span> ANY LENGTH RETURNING, interface covariance and inheritance rules, variable-length group display, and a DataBinder rule cluster (381 to 359).</p>
    <p><strong>Dropped from train 1013:</strong> the file-sharing default and fatal-I-O-status work (PB322 parts A and E), because the CI NIST guard job reads two new rows in <span class="mono">tests/nist/corpus.tsv</span> as legacy divergences. The branch is finished and gated; PB1955 (the guard) blocks it.</p>
    <p><strong>Partly done, by design:</strong> PB322 as above, the PB244 residues, PB1722 (a huge VALUE table compiles in 24 s and 2.5 GB), PB1042 (a dynamic-capacity table in an EXTERNAL record) and the OO universal-descriptor leg (PB480, PB1112).</p>
    <p><strong>Pacing:</strong> a new weekly quota began at 03:00; the target is one seventh of it per day, with the model sized to each group.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured this morning</h3>
      <p>Three waves moved 138 rows (GAP 497 to 359) for about 8.5 M subagent tokens, on Sonnet implementers with Opus landers; the weekly meter read 8% at 09:48.</p>
    </div>
    <div class="card">
      <h3>Owner decisions</h3>
      <p>Finish pending work first, then new work within the daily share of the weekly quota, keeping the five-hour session meter below its soft line. Fable is recommended where it would help and used only with the owner's explicit approval.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
