*>--- conformance-case
*> id: pb59-concat-alphabetic-to-boolean
*> rules: [AR-15.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.18.4 r3: "if argument-1 is usage display, then if argument-1 and
      *> all argument-2 are of class alphabetic, the function will return an
      *> ALPHABETIC value" - and 14.9.25.3 Table 16's Alphabetic row makes a
      *> boolean receiver "No". The result CATEGORY stays alphanumeric (the
      *> PIC A fold); the alphabetic-ness rides the BoundIntrinsicCall rider that
      *> only Table 16 consumes (PB59 family 7 / RV-15.18.4-3). CONCAT of two
      *> plain PIC X items into the same receiver is admitted - the r3
      *> "otherwise" arm - so this is the discriminating half of the pair.
      *> (2023 only: CONCAT itself is a 2023 function; below 2023 the program is
      *> rejected for the function, not for this rule.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGCAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A1 PIC A(2) VALUE "AB".
       01 WS-A2 PIC A(2) VALUE "CD".
       01 WS-B  PIC 1(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(WS-A1 WS-A2) TO WS-B
           STOP RUN.
