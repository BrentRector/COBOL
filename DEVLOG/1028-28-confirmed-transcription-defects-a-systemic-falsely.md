## Entry 1028 — 2026-07-26 19:40 PDT — 28 confirmed transcription defects, a systemic falsely-restrictive pattern across SEVEN directive notes — and I swept the wrong 105 pages

The figure-page sweep ran: 62 agents, 4.28M tokens, 18 minutes. **47 claims → 28 confirmed, 19 refuted**
(8 normative · 15 structural · 5 cosmetic). Findings are persisted per-agent under
`docs/rearchitecture/spec-reconciliation/` and merged by `scripts/spec/merge_reconciliation.py`.

**⛔ MY ERROR FIRST, because it bounds everything below.** I passed the page list to the workflow by TYPING it
rather than by using the computed file. What I typed was a plausible-looking regular sequence — 155, 157, 159 …
441 — and it was not the figure-page set. Result: **only 64 of the 173 target pages were actually swept, 105 pages
that are not figure pages were swept instead, and 109 genuine figure pages were never looked at.** The run
nonetheless reported "169 pages swept", which reads like success.

What caught it was `merge_reconciliation.py --expect 173` — a check that compares pages actually swept against the
intended set and fails loud on a gap. Without that expectation argument the sweep would have been filed as
complete. This is the fifth instance today of the same shape: a plausible value, accepted, failing somewhere that
does not name the cause. The remaining 109 pages are now running. The 28 findings below are real and were worth
the tokens; the COVERAGE claim was not.

**⛔ THE SYSTEMIC FINDING — seven directive figure notes are falsely restrictive.** Per §5.2.2/§5.2.3 an
underlined uppercase word is REQUIRED and a non-underlined one is OPTIONAL. The transcription's figure notes
misstate which words are underlined, and in one case invent a notation rule the standard does not contain:

- **FLAG-14** (line 4401) lists `ALL` and `ON` among the underlined/required words. Measured on a 300 dpi crop:
  zero ink beneath either, against a full-width rule beneath `OFF`. So the note makes `>> FLAG-14 EVALUATE`
  read as illegal and removes the implicit-ON path that §7.3.15.4 GR 2 depends on.
- **LEAP-SECOND** (line 4547) makes the same false claim about `ON`, so a bare `>>LEAP-SECOND` reads as illegal.
- **PROPAGATE** (line 4742) goes further and asserts *"In the compiler-directive formats the underlined
  alternative marks the default."* **No such rule exists.** It is falsified by LISTING, whose `OFF` is underlined
  while `>>LISTING ON` is the default (§7.3.18.3 GR 2), and it is contradicted by the transcription's own POP note
  ("`ALL` is not a default — underlining marks it as a required word"). Worse, §5.2.6.3 says an alternative
  containing only optional words IS the implicitly selected one — so a bare `>> PROPAGATE` means PROPAGATE **ON**,
  the exact opposite of what the invented rule yields.

The verifier identified the full affected set: FLAG-02 (4315), FLAG-14 (4401), LEAP-SECOND (4547), LISTING (4613),
PROPAGATE (4742), REF-MOD-ZERO-LENGTH (4837), TURN (4912). These are directives COBOL.NET implements, so this is
not an academic transcription issue — a parser built from these notes rejects legal source and gets defaults
backwards.

**The verification layer earned its cost.** 19 of 47 claims were refuted. The confirmations are not assertions
either: verifiers measured underline rules in pixels ("a full-width 2-px rule 14 px below the OFF baseline",
"the rule runs x=349..619, so the underlined token is LEAP-SECOND alone, not >>LEAP-SECOND"), corrected the
finders' citations (§8.3.1.1 → §5.2.2/§5.2.3), and tempered over-reach — on FLAG-14 the verifier noted that
`ALL` being un-underlined is more plausibly an ISO typesetting defect than a real optional-word marker, while
holding that the transcription's duty is to record the glyphs AS PRINTED and flag the anomaly rather than silently
"repair" it. That is exactly the standard set by `feedback_spec_diagrams_render_pdf`: record ISO's own defects,
never code around them.

**Persistence, added mid-run at the owner's direction** ("I don't want to lose content due to rate limits, or
session limits"). Every agent now writes its own JSON file before returning; a compare file with `findings: []` is
evidence a batch was swept clean, whereas an absent file is indistinguishable from work that never ran. The merge
script is safe to run mid-run and separates confirmed / refuted / **unverified**, marking unverified as not
actionable — a claim without an adversarial verdict is a hypothesis, and acting on one risks "fixing" correct
text. Two launch bugs were fixed getting here: args arriving as a JSON string (sliced as text, reported
"pages_swept: 835"), and the committed script returning from git as CRLF, which the Workflow permission layer
refuses as control characters without saying so.

Repairs for the 28 confirmed findings are the next step, and will land after the second sweep so the ON/OFF
directive notes can be corrected as one coherent group rather than piecemeal.

