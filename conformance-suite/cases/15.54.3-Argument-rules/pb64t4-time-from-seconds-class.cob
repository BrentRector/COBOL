*>--- conformance-case
*> id: pb64t4-time-from-seconds-class
*> rules: [AR-15.54.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.54.3 r1: "Argument-1 shall be a numeric value in standard numeric time form." An alphanumeric literal is
      *> not of class numeric - COBOLNET1627 (the class half of the §15.3 screen; kb/Work PB64 T4). 2014+: the function
      *> itself is a 2014 introduction (COBOLNET1502 below it - the locale-time-from-seconds-2014 construct row).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T4CLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS("47109") TO S.
           STOP RUN.
