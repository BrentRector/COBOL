*>--- conformance-case
*> id: pb134-divide-extra-into
*> rules: [FMT-14.9.12.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB134 - the ONE Format-2/Format-1 arithmetic operand discipline (COBOLNET1689): the
      *> GIVING forms print ONE sending operand with no ROUNDED; the non-GIVING forms print receiving
      *> identifiers only; every BY form of DIVIDE prints GIVING. The old binders silently dropped the
      *> extra operands / the ROUNDED, or crashed (DIVIDE's targets.Max on empty receivers).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB134N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4).
       01 B PIC 9(4).
       01 C PIC 9(4).
       01 D PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE A INTO B C GIVING D
           STOP RUN.
