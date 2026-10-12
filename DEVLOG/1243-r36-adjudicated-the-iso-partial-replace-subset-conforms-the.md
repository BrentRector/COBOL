## Entry 1243 — 2026-08-08 18:01 PDT — R36 adjudicated: the ISO partial-REPLACE subset conforms; the vendor spelling's silent half-parse becomes R39

The syn_copy:686 differential flip accused our partial-word replacement. Measured, the accusation
splits: the ISO subset — REPLACE LEADING/TRAILING with PSEUDO-TEXT operands, including the
empty-pseudo-text deletion — works end-to-end and is now pinned by the replace_partial_pseudo_text
golden (2002 corpus, second-REPLACE set-swap included). The failing shapes are the GCOS/ACU vendor
spellings — LITERAL partial-words and BY SPACES deletion — which no ISO edition ever admitted in
REPLACE (COPY's literal operands were what 2023 removed; REPLACE's were always pseudo-text). No
conformance gap; the vendor semantics stay a vendor-axis candidate, unimplemented by decision.

The probe walked into the adjacent real defect, now R39: the illegal REPLACE statement is silently
HALF-PARSED — ApplyReplaceStatements is static with no diagnostics path, applies nothing, reports
nothing, and the failure surfaces downstream as a misleading COBOLNET1639 on the never-replaced name.
The R34 shape, third sighting today.

Gate: the golden green by name at --std 2002. R36 landed as adjudicated; R39 opened; R33's
corpus+NIST ambiguity measurement still running in the background.

