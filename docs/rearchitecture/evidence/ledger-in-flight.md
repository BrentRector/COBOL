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
    <h3>In flight — Saturday 2026-10-03, evening</h3>
    <p><strong>Nothing is running.</strong> Seven trains landed since the last refresh, each CI-proven: <span class="pill good">1003</span> the DYNAMIC LENGTH STRUCTURE record image, SORT/MERGE short-record fill, RETURNING conformance and the preprocessor's COBOL separator set; <span class="pill good">1004</span> constant entries, the report-section grammar, conditional compilation and the SORT binder; <span class="pill good">1005</span> report binder clauses, the <span class="mono">&gt;&gt;COBOL-WORDS</span> retype at lex time and the reference resolver; <span class="pill good">1006</span> OO content-conformance, element-wise table MOVE and dynamic-length I-O limits; <span class="pill good">1007</span> one Annex C name fold for every word comparison, constant-entry forward references and the strongly typed group across INVOKE; <span class="pill good">1008</span> the condition binder and BY CONTENT arithmetic expressions into non-numeric formals; <span class="pill good">1009</span> SET senders, report SOURCE and VARYING operands, and intrinsic argument categories.</p>
    <p><strong>Partly done, by design:</strong> report rolled totals (PB1294), sum-counter families, the OO universal-descriptor leg (PB480, PB1112), the EVALUATE operator-subject half of PB1412, and the OR PAGE heading against a lower-level control footing (PB1927, where two general rules disagree and the record is not yet settled).</p>
    <p><strong>Next</strong> (after the weekly quota resets Sunday): a fresh fix wave from the computed clusters, a comprehensive battery (the last was #87), and the PB1527 goldens still sitting unlanded in an old worktree.</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions today</h3>
      <p><span class="mono">&gt;&gt;CALL-CONVENTION</span> stays COBOL-only unless a .NET mapping is added, and one may not be required (PB1945). The weekly ceiling for tonight was raised to 99%. Local-model use is limited to leads, indexes and drafts: nothing a local model writes enters the register or a golden without a Claude or human check.</p>
    </div>
    <div class="card">
      <h3>Measured today</h3>
      <p>GnuCOBOL 3.2.0 is now built in WSL and answers latitude questions by running. A hybrid of keyword and embedding retrieval finds the governing clause of a note in its top 10 about 82% of the time, against 71% for keyword search alone; whether that saves implementer turns is unmeasured.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
