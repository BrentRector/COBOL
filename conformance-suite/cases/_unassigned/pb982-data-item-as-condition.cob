*>--- conformance-case
*> id: pb982-data-item-as-condition
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB982 - ISO 8.8.4.2.1: "The simple conditions are the relation, boolean, class,
      *> condition-name, switch-status, sign, and omitted-argument conditions." WS-X is a PIC X data item:
      *> none of them, and no abbreviated relation is in progress to make it an object (8.8.4.12). It used
      *> to compile clean and abort the run unit with NotImplementedCobolFeatureException. Expected:
      *> COBOLNET2318 at every edition - in a PERFORM UNTIL, the condition position every verb shares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB982NDI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "5".
       PROCEDURE DIVISION.
           PERFORM UNTIL WS-X
               DISPLAY "NEVER"
           END-PERFORM
           STOP RUN.
