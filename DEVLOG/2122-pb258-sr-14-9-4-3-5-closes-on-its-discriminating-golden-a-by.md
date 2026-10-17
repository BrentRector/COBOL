## Entry 2122 — 2026-10-16 16:27 PDT — PB258: SR-14.9.4.3-5 closes on its discriminating golden — a BY REFERENCE address-identifier is sending beside a receiving identifier-2 in one CALL

kb/Work PB258 held one open inventory row, `SR-14.9.4.3-5` (§14.9.4.3 SR5: "If the BY REFERENCE phrase is
specified or implied for an identifier-2 and identifier-2 is not an address-identifier, it is a receiving
operand."). It was PARTIAL because the rule's exception arm had no source form: an address-identifier could not
be written as a CALL argument at all (COBOL0001).

**Re-probe.** That no longer reproduces. PB239 (`538562b36`) gave every identifier-2 slot the one
`addressIdentifier` operand, and `CallBinder.BindCall`'s BY REFERENCE arm binds it through `AddressArg` before the
receiving chokepoint, so it never meets `ExpressionBinder.ResolveReceiving`. That is what §8.4.3.11.3 SR5 and
§8.4.3.13.3 SR4 require ("This identifier format shall not be specified as a receiving operand"); Annex D.6.5.6.4
says the same in prose: "It behaves like being passed by content." No compiler change was needed, so the row
closes on evidence alone.

**Golden.** `conformance:2002/pb258_call_address_identifier_sending` is the closing condition the row named. One
statement passes an address-identifier and an ordinary identifier-2, both BY REFERENCE, first specified and then
implied (§14.9.4.4 GR5 in Format 1, GR9 a)1 in Format 2), and the callee stores into both formals.
- Format 1: N receives (`N=0015`, then `N=0020`), while the address formal's `SET … TO NULL` reaches nothing (the
  second call still reads `LR=XYZ`).
- Format 2, data arm: §14.9.4.3 SR10's class-pointer ban does not apply in Format 2, so both arguments are of class
  pointer and differ only in being an address-identifier. `P=NULL` after the first call, then `PA>XYZLO` beside
  `PP=NULL`.
- Format 2, program arm, the same shape over PROGRAM-POINTERs: `Q=NULL`, then `TARGET CALLED` beside `QP=NULL`.

The expected values were derived from §14.9.4.3 SR4/SR5, §14.2.3 GR8 and §8.4.3.11.4 GR1 before the run. The
output is identical at `--std` 2002, 2014 and 2023. Below 2002 the edition gate refuses the operand
(`negative/pb239-call-address-identifier-at-85`), so I added no new negative.

**Sibling sweep.** Only one other clause states the address-identifier exception: INVOKE's §14.9.23.3 SR19/SR20,
whose rows are already CONFORMS with their own witnesses. While reading `85/l1_call_by_reference_receiving_operand`
I found an uncited paraphrase of §14.2.3 in its header and replaced it with GR8's checked text.

**Rows.** `SR-14.9.4.3-5` PARTIAL → CONFORMS (GAP 65 → 64), and PB258's `closes_rows` names it. PB245's
`closes_rows` back-link and `ClosesRowsBackLinkDriftTests` answer the design question the note carried.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN ===` (run `20261016T225508Z-ea4b54`; the new golden and the edited 85 golden both Passed in leg 1).

