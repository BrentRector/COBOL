*>--- conformance-case
*> id: pb183-usage-pointer-group-usage
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 reached through 13.18.60.4 GR1 - THE DERIVED CONSEQUENCE. GR1: "If the USAGE
*> clause is specified or implied at a group level, it applies only to each elementary item in the
*> group." So G's POINTER phrase applies to A and B, each at level 05 and subordinate to an ordinary
*> group - SR14 violations both. The verdict is reported ONCE, at the entry carrying the clause.
*>
*> This fixture is also the arm the FIRST build of the screen silently missed: DataItem.IsGroup is
*> `Pic is null && Children.Count > 0`, and ParseUsage synthesizes a pointer profile onto the header
*> before its subordinates are known, so an IsGroup-keyed test waved G through and then blessed it as
*> "a level-1 elementary pointer". The subject test is the spec's own notion instead - an entry with
*> subordinates is a group (8.5.1.3.2).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183E.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G USAGE POINTER.
   05 A PIC X.
   05 B PIC X.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHABLE".
    STOP RUN.
