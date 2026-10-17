*>--- conformance-case
*> id: r03-hex-national-literal-at-85
*> rules: [GR-8.3.3.5.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> The hexadecimal-national literal NX"..." is ISO 8.3.3.5.2 Format 2 of a
      *> NATIONAL literal, and national data is a COBOL-2002 introduction - so it
      *> must be rejected at COBOL-85 exactly as N"..." is (fix-queue R03).
      *>
      *> THIS IS THE HALF A NEW TOKEN WOULD HAVE BROKEN. The fix folds Format 2
      *> into the existing NATLIT token, because 8.3.3.5.4 GR2 is an ALL-FORMATS
      *> rule ("National literals are of the class and category national"). One
      *> consequence is that the 2002 introduction gate keeps firing with no edit
      *> at all - the pass keys on the token, and Format 2 IS that token.
      *> A separate NATHEXLIT token would have entered the gate's blind spot and
      *> compiled clean at COBOL-85 (feedback_edition_gate_sweep).
      *>
      *> Note there is no national PICTURE anywhere in this program: the gate
      *> being proven is the LITERAL's, not the data item's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R03HEX85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(NX"00410042") TO L.
           DISPLAY "L=" L.
           STOP RUN.
