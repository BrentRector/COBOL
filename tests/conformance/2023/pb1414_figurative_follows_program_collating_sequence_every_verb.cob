      *> PB1414 sweep - ISO 8.3.3.6.4 GR7: "At runtime, when referenced outside the SPECIAL-NAMES paragraph, the
      *>   low-value format represents the character, or multiple-character combination, that has the lowest
      *>   ordinal position in the runtime collating sequence."  (cite.py --check 8.3.3.6.4 -> OK 7)
      *> GR1: "When a figurative constant is used in a context requiring national characters, the figurative
      *>   constant represents a national character value."  (cite.py --check 8.3.3.6.4 -> OK 1)
      *> The alphanumeric program collating sequence AL orders "B" first, the NATIONAL one (NAL) orders "C"
      *> first, so every verb that reads LOW-VALUE must answer "B" for an alphanumeric context and "C" for a
      *> national one - never the native U+0000 and never the other class's answer. The two alphabets differ on
      *> purpose: a national context that took the alphanumeric sequence (or the reverse) is a visible failure.
      *> WHY EACH LEG CAN FAIL:
      *>  MX/MN/MNG/MAG/MXE/MREF - the MOVE receivers: elementary alphanumeric, elementary national, a
      *>      GROUP-USAGE NATIONAL group (national by GR1), an alphanumeric group, an alphanumeric-edited item
      *>      (BB B: the B insertion character is a space) and a reference-modified slice.
      *>  INT   - INITIALIZE ... REPLACING ALPHANUMERIC BY LOW-VALUE NATIONAL BY LOW-VALUE.
      *>  STR/STRN/UNS - STRING (both classes) and UNSTRING DELIMITED BY LOW-VALUE (kb/Work PB1185).
      *>  CMP/SRCH/EV/88 - relation, SEARCH WHEN, EVALUATE WHEN and a level-88 VALUE LOW-VALUE.
      *>  IBEF/IBEFN/IAFTN/ICONV/ICONVN/IREP/IREPN/GBEF/GALL - INSPECT BEFORE/AFTER/CONVERTING/REPLACING and a
      *>      TALLYING operand, over alphanumeric identifier-1, national identifier-1 and a GROUP-USAGE NATIONAL
      *>      identifier-1 (the group has no PICTURE, so its class - national - must decide, kb/Work PB1414).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1414SWP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR ALPHANUMERIC IS AL
                                      FOR NATIONAL IS NAL.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" "A" "C"
           ALPHABET NAL FOR NATIONAL IS N"CAB".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(3).
       01 XI PIC X(5).
       01 NA PIC N(3).
       01 NI PIC N(5).
       01 NG GROUP-USAGE NATIONAL.
          05 NGA PIC N(5).
       01 AG.
          05 AGA PIC X(3).
       01 XE PIC XXBX.
       01 XR PIC X(6) VALUE "AAAAAA".
       01 TBL.
          05 TE OCCURS 3 INDEXED BY IX PIC X.
          05 TN OCCURS 3 PIC N.
       01 I PIC 9.
       01 R PIC X(10).
       01 RN PIC N(10).
       01 PTR PIC 99.
       01 N PIC 99.
       01 W PIC X VALUE "B".
          88 ISLOW VALUE LOW-VALUE.
       PROCEDURE DIVISION.
           MOVE LOW-VALUE TO XA. DISPLAY "MX=" XA.
           MOVE LOW-VALUE TO NA. DISPLAY "MN=" NA.
           MOVE LOW-VALUE TO NG. DISPLAY "MNG=" NG.
           MOVE LOW-VALUE TO AG. DISPLAY "MAG=" AG.
           MOVE LOW-VALUE TO XE. DISPLAY "MXE=" XE.
           MOVE LOW-VALUE TO XR(2:3). DISPLAY "MREF=" XR.
           INITIALIZE TBL REPLACING ALPHANUMERIC BY LOW-VALUE
                                     NATIONAL BY LOW-VALUE.
           DISPLAY "INT=" TE(1) TE(2) "/" TN(1) TN(2).
           MOVE SPACES TO R.
           STRING LOW-VALUE DELIMITED SIZE INTO R.
           DISPLAY "STR=" R.
           MOVE SPACES TO RN.
           STRING LOW-VALUE DELIMITED SIZE INTO RN.
           DISPLAY "STRN=" RN.
           MOVE "ABCBA" TO R.
           MOVE 1 TO PTR.
           UNSTRING R DELIMITED BY LOW-VALUE INTO XA WITH POINTER PTR.
           DISPLAY "UNS=" XA "/" PTR.
           MOVE "BAC" TO XA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
              IF XA(I:1) = LOW-VALUE DISPLAY "CMP" I END-IF
           END-PERFORM.
           SET IX TO 1.
           SEARCH TE
              WHEN TE(IX) = LOW-VALUE DISPLAY "SRCH"
           END-SEARCH.
           EVALUATE XA(1:1) WHEN LOW-VALUE DISPLAY "EV-Y"
              WHEN OTHER DISPLAY "EV-N" END-EVALUATE.
           IF ISLOW DISPLAY "88Y" ELSE DISPLAY "88N" END-IF.
           DISPLAY "DSP=" LOW-VALUE "=".
           MOVE "AZBZA" TO XI.
           MOVE N"AZCZA" TO NI.
           MOVE N"AZCZA" TO NGA.
           MOVE 0 TO N.
           INSPECT XI TALLYING N FOR ALL "Z" BEFORE LOW-VALUE.
           DISPLAY "IBEF=" N.
           MOVE 0 TO N.
           INSPECT NI TALLYING N FOR ALL N"Z" BEFORE LOW-VALUE.
           DISPLAY "IBEFN=" N.
           MOVE 0 TO N.
           INSPECT NI TALLYING N FOR ALL N"Z" AFTER LOW-VALUE.
           DISPLAY "IAFTN=" N.
           MOVE 0 TO N.
           INSPECT NG TALLYING N FOR ALL N"Z" BEFORE LOW-VALUE.
           DISPLAY "GBEF=" N.
           MOVE 0 TO N.
           INSPECT NG TALLYING N FOR ALL LOW-VALUE.
           DISPLAY "GALL=" N.
           INSPECT XI CONVERTING "Z" TO LOW-VALUE.
           DISPLAY "ICONV=" XI.
           INSPECT NI CONVERTING N"Z" TO LOW-VALUE.
           DISPLAY "ICONVN=" NI.
           MOVE "AZBZA" TO XI.
           INSPECT XI REPLACING ALL "Z" BY LOW-VALUE.
           DISPLAY "IREP=" XI.
           MOVE N"AZCZA" TO NI.
           INSPECT NI REPLACING ALL N"Z" BY LOW-VALUE.
           DISPLAY "IREPN=" NI.
           STOP RUN.
