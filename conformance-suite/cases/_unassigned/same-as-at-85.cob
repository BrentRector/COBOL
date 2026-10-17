*>--- conformance-case
*> id: same-as-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.49 - the SAME AS clause is a COBOL-2002 introduction (the TYPEDEF-family
      *> data-description edge, provisional per the decision-1 policy): at --std 85 the version-conformance
      *> pass rejects it (COBOLNET0900, registry row same-as-clause-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAME-AS-AT-85-P10TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PROTO PIC 9(3).
       01 W SAME AS PROTO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 7 TO W.
           DISPLAY W.
           STOP RUN.
