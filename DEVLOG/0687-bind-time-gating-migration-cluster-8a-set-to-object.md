## Entry 687 — 2026-07-08 04:23 PDT — Bind-time gating migration Cluster 8a — SET … TO object-reference (Format 5)

Cluster 8 is split (it carries the SET ANTLR-selection risk); 8a does SET … TO object-reference (§14.9.39 Format 5,
2002) alone. Ungated `setObjectReferenceStatement` (`CobolParserCore.g4:1083`) and added the introduction `Check` at
`OoBindSetObjectRef` — the ONE convergence point for both the NULL/SELF/SUPER route (BindSet) and the data-sender
re-route (BindSetTo). Retired all three set-object-ref `EditionGateHints` arms, INCLUDING the interim `SET`-token arm
that Cluster 1 had to add (the fixture's 0900 no longer depends on a parse error — it is now a clean bind-time gate).
**AMBIGUITY (the recon's "mild" risk) cleared:** ungating leaves NULL/SELF/SUPER senders reaching
setObjectReferenceStatement; the FULL legacy guard is byte-identical and a valid `SET IX TO 1` / `SET IX UP BY 1` /
`SET cond TO TRUE` at 85 still parses (checked). The set-object-reference matrix fixture (`01 W USAGE OBJECT
REFERENCE` + `SET W TO NULL`) at 85 now emits two clean bind-time 0900s (the USAGE decl + the SET), both correct.
Verified: SET … TO NULL → COBOLNET0900 at 85, compiles at 2002. Battery: conformance 2055 · unit 224 · FULL legacy
guard NIST 353 MATCH.

