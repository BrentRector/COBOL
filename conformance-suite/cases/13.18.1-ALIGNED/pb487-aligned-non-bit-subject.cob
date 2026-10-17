*>--- conformance-case
*> id: pb487-aligned-non-bit-subject
*> rules: [SR-13.18.1.3-1, FMT-13.18.1.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  A is an ALPHANUMERIC group item (no GROUP-USAGE BIT clause, and
      *> none inherited), so the clause has no defined subject here: 13.18.1.4 GR1 states its whole effect in
      *> BITS ("aligned on the first bit of the first available byte boundary", with implicit filler bits per
      *> 8.5.1.6.3), which is meaningless for a byte-aligned alphanumeric group.  Accepting it inert is the
      *> silent-wrong-layout outcome that rule exists to prevent, so it is refused: COBOLNET1942.
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002, so at COBOL-85 the word is a
      *> user-defined word and this source is a different program - the 2002 introduction gate (COBOLNET0900,
      *> constructs.json row aligned-clause-2002) is what answers there, and the edition-matrix row owns it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A ALIGNED.
          05 B PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
