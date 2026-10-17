*>--- conformance-case
*> id: pb780-constant-as-constant-cycle
*> rules: [SR-13.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - a constant entry's literal-1 may be another constant-name (13.10.3 SR2;
      *> conformance/2002/pb780_constant_names_slot_witnesses), but 13.10.3 SR5 forbids the value of literal-1 to
      *> "be dependent, directly or indirectly, upon the value of constant-name-1": KA is AS KB and KB is AS KA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780CYC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KA CONSTANT AS KB.
       01  KB CONSTANT AS KA.
       PROCEDURE DIVISION.
           DISPLAY KA.
           STOP RUN.
