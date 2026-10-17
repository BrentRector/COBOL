*>--- conformance-case
*> id: pb395-turn-data-incompatible-below-2002
*> rules: [GR-14.9.17.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> The edition gate under conformance:cases/14.9.17-GO-TO/pb395_goto_depending_incompatible. §14.9.17.4 GR2's
      *> EC-DATA-INCOMPATIBLE sentence is only OBSERVABLE where checking can be enabled, and the §7.3
      *> compiler-directive facility — >>TURN (§7.3.25) — is a COBOL-2002 introduction, so the same program is
      *> refused at 1985. The transfer and fall-through halves of GR2 are edition-independent and are exercised by
      *> the positive golden; this fixture exists so the 2002 floor of the EC half cannot drift unnoticed.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB395TURNGATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SEL PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO P-A DEPENDING ON SEL.
           STOP RUN.
       P-A.
           DISPLAY "A".
           STOP RUN.
