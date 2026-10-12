## Entry 686 — 2026-07-08 04:09 PDT — Bind-time gating migration Cluster 7 — CLASS-ID + INTERFACE-ID compilation units

Seventh cluster; the two OO compilation-unit headers (CLASS-ID / INTERFACE-ID — dedicated `_ID` tokens, hard-reserved
at all editions). Ungated the compilationGroup alt `(programUnit | classDefinition | interfaceDefinition)+`
(`CobolParserCore.g4:145`) and added the introduction `Check` in `OoClassTable.Build`: interface loop after `iname`
(`Constructs.InterfaceDefinition2002`), class loop after `name` (`Constructs.ClassDefinition2002`), both gated 2002.
Deleted the ONE **shared** `EditionGateHints` IDENTIFICATION/CLASS_ID/INTERFACE_ID lookahead arm (both constructs
migrated together, as it served both). Beyond-recipe: `using CobolNet.Editions;` in `OoClassTable.cs`. No ambiguity —
compilationGroup alts disambiguate on the leading `_ID` token past the optional `IDENTIFICATION DIVISION`.

**Two payoffs of moving to the recognition point:** (1) the message now names the UNIT (e.g. `class definition
'EGD6' …`), which the parse-layer signature could not; (2) the `SUPPRESS PRINTING.`@85 case — one of the original
DEVLOG-679 R1 false-positives, where the speculative compilationGroup class/interface gate leaked "an interface
definition requires COBOL-2002" onto an unrelated unsupported statement — is now genuinely NEUTRAL (generic
`COBOL0001`), because the shared reverse signature is gone. Verified: CLASS-ID/INTERFACE-ID → COBOLNET0900 at 85,
compile at 2002; SUPPRESS neutral; JSON still → COBOL0313. Battery: conformance 2055 · unit 224 · FULL legacy guard
NIST 353 MATCH.

