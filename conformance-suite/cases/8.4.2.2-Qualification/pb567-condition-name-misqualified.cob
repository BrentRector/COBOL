*>--- conformance-case
*> id: pb567-condition-name-misqualified
*> rules: [SR-8.4.2.2.3-4, SR-8.4.2.2.3-5, SR-13.16.3-23]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB567 - the NEGATIVE side of ISO 13.16.3 SR23 ("Each condition-name is subordinate to the
      *> data-name with which it is associated") with 8.4.2.2.3 SR4/SR5: IS-A's conditional variable S is
      *> subordinate to G1 only, so qualifying it by the unrelated record H1 identifies NO condition-name.
      *> Before the fix this was reported as "'IS-A' is not defined - no declaration gives the name", and in
      *> EVALUATE as a Table-15 pairing error about an identifier. Edition-independent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB567NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 S PIC X VALUE "A".
             88 IS-A VALUE "A".
       01 H1.
          05 HX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           IF IS-A OF H1 DISPLAY "H1" END-IF
           STOP RUN.
