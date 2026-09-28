      *> kb/Work PB1117 — ISO §14.6.13.2 rule 2 ON THE ITEM-IDENTIFICATION LANE: a numeric data item used as a
      *> SUBSCRIPT, a REFERENCE-MODIFICATION position, or the OCCURS DEPENDING object is a numeric SENDING item
      *> referenced during the execution of the statement, so invalid content in it sets EC-DATA-INCOMPATIBLE —
      *> and it does so INSIDE a class condition too, because the class-condition exemption covers the item the
      *> test examines, never the subscript that locates it.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.6.13.2 "When the content of a numeric sending item that is not described with a
      *>     standard floating-point usage is referenced during the execution of a statement and the content of
      *>     that sending operand would evaluate to false in a numeric class condition" -> OK §14.6.13.2 2)
      *>   cite.py --check 14.6.13.2 "The EC-DATA-INCOMPATIBLE exception condition is set to exist for a class
      *>     condition and a VALIDATE statement when invalid data is detected during item identification"
      *>     -> OK §14.6.13.2 1)  (NOTE 1: "a subscript reference during a class test could cause the
      *>     EC-DATA-INCOMPATIBLE exception condition to exist")
      *>   cite.py --check 8.4.2.3.4 "the subscript is the result of the evaluation of arithmetic-expression-1"
      *>     -> OK §8.4.2.3.4 1) b) — the subscript is the item's VALUE: its sign and its P scaling count.
      *>
      *> The group MOVEs plant character content in G, so N/K/S/NP are stored as their character images (the
      *> only storage that can hold content failing the class test). >>TURN is COBOL-2002 (§7.3.25);
      *> negative/w70c-pb1117-turn-below-2002 pins its refusal below that.
      *>
      *> WHY EACH LEG CAN FAIL (the pre-PB1117 build decoded every one of these images through a tolerant digit
      *> scan, raised nothing on legs 1-4, read S = -1 as occurrence 1 and NP = 20 as occurrence 2):
      *>   1  MOVE TE(N) TO X with N = "A"             -> RAISED EC-DATA-INCOMPATIBLE; X unchanged (ABCDE).
      *>   2  IF TE(N) IS ALPHABETIC (the same N)      -> RAISED EC-DATA-INCOMPATIBLE (rule 1's NOTE 1).
      *>   3  MOVE X(N:1) TO X                         -> RAISED EC-DATA-INCOMPATIBLE.
      *>   4  MOVE OG TO X with the ODO object K = "A" -> RAISED EC-DATA-INCOMPATIBLE.
      *>   5  valid N = 3, K = 2: DISPLAY TE(N) -> C, and OG's current two occurrences -> [AB   ]; no raise.
      *>   6  S = -1: DISPLAY TE(S) -> RAISED EC-BOUND-SUBSCRIPT (§8.4.2.3.4 2): "less than one").
      *>   7  NP PIC 9P = 20: DISPLAY TE(NP) -> T, the twentieth occurrence.
      *>   8  CNT = 05 — five conditions, exactly one per raising leg.
       >>TURN EC-DATA-INCOMPATIBLE EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CPB1117.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9.
          05 K PIC 9.
          05 S PIC S9.
          05 NP PIC 9P.
       01 OG.
          05 OE PIC X OCCURS 1 TO 5 DEPENDING ON K.
       01 TX VALUE "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123".
          05 TE PIC X OCCURS 30.
       01 X PIC X(5) VALUE "ABCDE".
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE
                                         EC-BOUND-SUBSCRIPT.
       D1P.
           ADD 1 TO CNT
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "A3" TO G
           MOVE TE(N) TO X
           DISPLAY "1 X=" X
           IF TE(N) IS ALPHABETIC
               DISPLAY "2 ALPHABETIC"
           END-IF
           DISPLAY "2 DONE"
           MOVE X(N:1) TO X
           DISPLAY "3 X=" X
           MOVE "AB" TO OG
           MOVE "3A" TO G
           MOVE OG TO X
           DISPLAY "4 X=" X
           MOVE "32" TO G
           MOVE OG TO X
           DISPLAY "5 " TE(N) " [" X "]"
           MOVE -1 TO S
           DISPLAY "6 " TE(S)
           DISPLAY "6 DONE"
           MOVE 20 TO NP
           DISPLAY "7 " TE(NP)
           DISPLAY "8 CNT=" CNT
           STOP RUN.
