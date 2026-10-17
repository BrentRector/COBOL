*>--- conformance-case
*> id: pb442_forced_national_leaf_group_move
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB442 - a numeric item of USAGE NATIONAL that a figurative fill or a reference-modified
      *> store forces to its character image (MoveBinder's two carriage marks) still moves through a
      *> group MOVE exactly as an unforced one does.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.25.4 GR4 - a move that is not an elementary move "is treated exactly as if it were an
      *>    alphanumeric to alphanumeric elementary move, except that there is no conversion of data
      *>    from one form of internal representation to another"; national group items are treated as
      *>    elementary items. So MOVE XA TO XB copies XA's twelve bytes into XB (XR = XS afterwards and
      *>    each XB leaf holds the XA leaf's value), and MOVE NC TO NA copies NC's national characters.
      *>  14.9.25.4 GR1 - an elementary MOVE of 555 / 321 / 666 / 777 stores that value; ADD 1 to 321
      *>    gives 322 (14.9.2).
      *>  The LOW-VALUE / HIGH-VALUE fills and the space slice store only set up the forced leaves (their
      *>    content is replaced before anything reads it), so they decide no expected value.
      *>
      *> WHY THIS CAN FAIL: the forced leaf's carrier is its character image while its twin keeps the
      *> native carrier; a group MOVE that copied one carrier into the other through the wrong width
      *> would leave the receiver holding the wrong digits or break XR = XS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442FNG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA.
          05 XA1 PIC 9(3) USAGE NATIONAL VALUE 111.
          05 XA2 PIC 9(3) USAGE NATIONAL VALUE 222.
       01 XR REDEFINES XA PIC X(12).
       01 XB.
          05 XB1 PIC 9(3) USAGE NATIONAL VALUE 333.
          05 XB2 PIC 9(3) USAGE NATIONAL VALUE 444.
       01 XS REDEFINES XB PIC X(12).
       01 NA GROUP-USAGE NATIONAL.
          05 NA1 PIC N(2) VALUE N"AB".
          05 NA2 PIC 9(3) VALUE 123.
       01 NB GROUP-USAGE NATIONAL.
          05 NB1 PIC N(2) VALUE N"CD".
          05 NB2 PIC 9(3) VALUE 456.
       01 NC GROUP-USAGE NATIONAL.
          05 NC1 PIC N(2) VALUE N"EF".
          05 NC2 PIC 9(3) VALUE 789.
       PROCEDURE DIVISION.
       M.
           MOVE LOW-VALUE TO XA1
           MOVE SPACE TO XB2(2:1)
           MOVE 555 TO XA1
           MOVE XA TO XB
           DISPLAY "XB1=" XB1 " XB2=" XB2
           IF XR = XS DISPLAY "XR=XS" ELSE DISPLAY "XR-NOT-XS".
           MOVE 666 TO XB2
           MOVE 777 TO XB1
           MOVE XB TO XA
           DISPLAY "XA1=" XA1 " XA2=" XA2
           MOVE LOW-VALUE TO NA2
           MOVE HIGH-VALUE TO NB2
           MOVE NC TO NA
           DISPLAY "NA2=" NA2
           MOVE 321 TO NC2
           MOVE NC TO NB
           DISPLAY "NB2=" NB2
           MOVE NB TO NA
           ADD 1 TO NA2
           DISPLAY "NA2=" NA2
           STOP RUN.
