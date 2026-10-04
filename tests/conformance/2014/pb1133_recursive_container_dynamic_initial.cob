      *> kb/Work PB1133 - THE INITIAL-STATE ACTIONS OF A RECURSIVE PROGRAM THAT CONTAINS PROGRAMS: DYNAMIC-LENGTH ITEMS
      *>   AND THE PROGRAM COLLATING SEQUENCE.
      *>   14.6.2.3.2 action 7: "The length of each dynamic-length elementary item that is specified without a VALUE
      *>     clause is set to zero." - and an item WITH a VALUE returns to that value's length; the same occasions as
      *>     every initial-state action (case 1 first activation, case 3 after a CANCEL of the program or of a
      *>     program that contains it). 14.6.2.3.3: otherwise the static data keeps its last-used state.
      *>   14.6.2.3.2 (action 1-8 lead-in): "Before data is placed in the initial state, the initial alphanumeric and
      *>     national program collating sequences are determined as specified in 12.3.6", so LOW-VALUE in a VALUE clause
      *>     is the FIRST character of the program collating sequence: ALPHABET S1 IS "B" "A" makes it "B"
      *>     (12.3.7.4 GR9), on the first activation and again after the CANCEL.
      *>   11.10.4 GR4: the contained program is recursive too, so its working-storage is static as well.
      *>   Derived trace: call 1 - DL has length 0, DLV 3 (VALUE "abc"), LV is BB; the contained program's IDL length 0.
      *>     The program then sets DL to 5 characters, DLV to 1, LV to ZZ and IDL to 4. Call 2 (last-used) shows 5 1 ZZ
      *>     and 4. CANCEL puts everything back: call 3 shows 0 3 BB and 0 again.
      *>   Each leg can fail: refused outright; a dynamic length kept across the CANCEL; LV shows the plain LOW-VALUE
      *>     character instead of BB (the sequence not determined before the initial state).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133D.
       PROCEDURE DIVISION.
           CALL "CD1133"
           CALL "CD1133"
           CANCEL "CD1133"
           CALL "CD1133"
           STOP RUN.
       END PROGRAM PB1133D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CD1133 RECURSIVE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S1 IS "B" "A".
       OBJECT-COMPUTER.
           CD1133 PROGRAM COLLATING SEQUENCE IS S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DL PIC X DYNAMIC LENGTH.
       01 DLV PIC X DYNAMIC LENGTH VALUE "abc".
       01 LV PIC XX VALUE LOW-VALUE.
       PROCEDURE DIVISION.
           DISPLAY "LEN=" FUNCTION LENGTH (DL) " "
               FUNCTION LENGTH (DLV) " LV=" LV
           MOVE "zzzzz" TO DL
           MOVE "q" TO DLV
           MOVE "ZZ" TO LV
           CALL "CI1133"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CI1133.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDL PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           DISPLAY "I LEN=" FUNCTION LENGTH (IDL)
           MOVE "wwww" TO IDL
           GOBACK.
       END PROGRAM CI1133.
       END PROGRAM CD1133.
