*>--- conformance-case
*> id: pb549-program-address-below-2002
*> rules: [GR-13.18.60.4-24]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB549 — the BELOW-INTRODUCTION half. §14.9.39.2 Format 9 (program-pointer-assignment) and the
      *> §8.4.3.13 program-address-identifier that fills it are the COBOL-2002 program-pointer family: §8.5.2.9
      *> category program-pointer, §13.18.60.4 GR24's USAGE PROGRAM-POINTER item and PROGRAM-POINTER's own
      *> §8.9 reservation all arrive together at 2002. So at --std 85 this program names its edition
      *> (COBOLNET0900, constructs.json set-program-pointer-2002 beside usage-program-pointer-2002) rather
      *> than drawing the bare `COBOL0001: cannot parse construct near 'PROGRAM'` it drew at EVERY edition
      *> before the general format had a grammar rule at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549B85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM "PB549NS"
           STOP RUN.
