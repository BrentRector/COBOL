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
    <h3>In flight — Sunday 2026-10-04, midday</h3>
    <p><strong>Nothing is gated or landing right now.</strong> Four trains have landed since the weekly reset, each CI-proven: <span class="pill good">1011</span> STRING and UNSTRING operand screens, the OO data-division placement table, method BY VALUE formals, constant-name integer positions and TYPEDEF RENAMES (GAP 497 to 426); <span class="pill good">1012</span> linkage rules for function formals, SORT and MERGE record-size screens, the FLAG-02 and FLAG-14 detectors and the binder driver (426 to 381); <span class="pill good">1013</span> ANY LENGTH RETURNING, interface covariance and inheritance rules, variable-length group display and a DataBinder rule cluster (381 to 359); <span class="pill good">1014</span> extended letters in COBOL words with one Annex C name fold, the parser-core suffix-order and SUPER grammar, and the OO binder's receiver and selector rules (359 to 336).</p>
    <p><strong>Dropped and waiting:</strong> the file-sharing default and fatal-I-O-status determinations of PB322 (the CI NIST guard job reads two new rows in <span class="mono">tests/nist/corpus.tsv</span> as legacy divergences; PB1955 blocks it).</p>
    <p><strong>Tooling moved this morning:</strong> the local WSL distro is now Ubuntu 26.04.1 and passed its first Linux gate; CI is being pinned to the same <span class="mono">ubuntu-26.04</span> image so the two stop drifting apart. ANTLR is told to read grammars as UTF-8 (the cause of train 1007's CI-only red). PB1957 lists every other place CI and the local gates differ.</p>
    <p><strong>Partly done, by design:</strong> PB322 as above, PB1425 (SELF and SUPER identifier positions), the PB1136 receiver half, the PB244 residues, PB1722, PB1042 and the OO universal-descriptor leg (PB480, PB1112).</p>
    <p><strong>Pacing:</strong> one seventh of the weekly quota per day is the target; four waves have been spent on Sonnet and Opus implementers sized per group.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Measured today</h3>
      <p>Four trains moved 161 rows (GAP 497 to 336) for about 9.9 M subagent tokens: the Sonnet-led waves cost about 3 M each, the mixed Sonnet and Opus wave 1.4 M.</p>
    </div>
    <div class="card">
      <h3>Owner decisions</h3>
      <p>Finish pending work first, then new work inside the daily share of the weekly quota, keeping the five-hour session meter below its soft line. Make CI and the local gates the same build where possible, and reproduce each remaining difference locally. Fable only with explicit approval.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
