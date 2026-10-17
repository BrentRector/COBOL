*>--- conformance-case
*> id: undefined-name-reference
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R30 - a name declared NOWHERE, referenced as a MOVE sender, compiled with zero
      *> diagnostics and threw NotImplementedCobolFeatureException at run time - in EVERY reference
      *> position measured. ISO 8.4.2.1: "a statement shall contain a reference that uniquely
      *> identifies that resource"; a typo is never a feature gap. Edition-independent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R30NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
           MOVE NO-SUCH-NAME TO WS-R.
           DISPLAY WS-R.
           STOP RUN.
