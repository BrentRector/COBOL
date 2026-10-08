      *> kb/Work PB1931 - ISO/IEC 1989:2023 14.2.3 GR8: "If the argument is passed by reference, the activated
      *> runtime element operates as if the formal parameter occupies the same storage area as the argument."
      *> ADDRESS OF the formal is therefore the address of the caller's argument (8.4.3.11.4 GR1: the pointer
      *> "contains the address of identifier-1"), two pointers are equal when "they reference the same address"
      *> (8.8.4.2.16), and a BASED item placed at the returned pointer writes the caller's storage.
      *> cite.py --check 14.2.3 "occupies"                       -> OK 14.2.3 8)
      *> cite.py --check 8.8.4.2.16 "same address"               -> OK 8.8.4.2.16
      *> cite.py --check 8.4.3.11.4 "contains the address of"    -> OK 8.4.3.11.4 1)
      *> BY CONTENT (14.2.3 GR9) is the contrast: the formal is a record allocated by the activating element,
      *> so its address is NOT the argument's and a store through it leaves the argument alone.
      *> The arms: an alphanumeric item, a signed DISPLAY numeric, a binary numeric, a group, a table element,
      *> a reference-modified item, a formal forwarded to a second program, a METHOD formal (typed and
      *> universal object reference) and a user-defined FUNCTION formal. Every one printed DIFF or trapped
      *> EC-BOUND-PTR when the note was filed (before the area formals of PB2087 / PB2089).
      *> DERIVATION: each SAME line is the equality above; each store through the BASED view lands in the
      *> argument: ARG "CHANGED!"; the unsigned DISPLAY numeric 4321; the binary 7 -> 00000007; the group's
      *> first four characters "WXYZ" keep 5678; table element 2 "WXYZ" between "ABCD"s; reference
      *> modification (3:4) "wxyz" inside "OR....AL". Unsigned items keep the sign-encoding choice out of it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1931.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(8).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING LA RETURNING RP.
           SET RP TO ADDRESS OF LA
           GOBACK.
       END FUNCTION F1931.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K1931
           FUNCTION F1931.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ARG   PIC X(8) VALUE "ORIGINAL".
       01 NARG  PIC 9(4) VALUE 1234.
       01 CARG  PIC 9(8) COMP VALUE 99.
       01 GARG.
          05 G1 PIC X(4) VALUE "ABCD".
          05 G2 PIC 9(4) VALUE 5678.
       01 TBL.
          05 TE PIC X(4) OCCURS 3 TIMES VALUE "ABCD".
       01 RM    PIC X(8) VALUE "ORIGINAL".
       01 O     USAGE OBJECT REFERENCE K1931.
       01 U     USAGE OBJECT REFERENCE.
       01 WP    USAGE POINTER.
       01 WP2   USAGE POINTER.
       01 VIEW  PIC X(8) BASED.
       01 NVIEW PIC 9(4) BASED.
       01 CVIEW PIC 9(8) COMP BASED.
       01 GVIEW BASED.
          05 V1 PIC X(4).
          05 V2 PIC 9(4).
       01 TVIEW PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET WP2 TO ADDRESS OF ARG
           CALL "P1931SUB" USING ARG RETURNING WP
           IF WP = WP2 DISPLAY "A-SAME" ELSE DISPLAY "A-DIFF" END-IF
           SET ADDRESS OF VIEW TO WP
           MOVE "CHANGED!" TO VIEW
           DISPLAY "ARG=" ARG

           MOVE "ORIGINAL" TO ARG
           CALL "P1931SUB" USING BY CONTENT ARG RETURNING WP
           IF WP = WP2 DISPLAY "K-SAME" ELSE DISPLAY "K-DIFF" END-IF
           SET ADDRESS OF VIEW TO WP
           MOVE "COPYONLY" TO VIEW
           DISPLAY "ARG=" ARG

           SET WP2 TO ADDRESS OF NARG
           CALL "P1931SUBN" USING NARG RETURNING WP
           IF WP = WP2 DISPLAY "N-SAME" ELSE DISPLAY "N-DIFF" END-IF
           SET ADDRESS OF NVIEW TO WP
           MOVE 4321 TO NVIEW
           DISPLAY "NARG=" NARG

           SET WP2 TO ADDRESS OF CARG
           CALL "P1931SUBC" USING CARG RETURNING WP
           IF WP = WP2 DISPLAY "C-SAME" ELSE DISPLAY "C-DIFF" END-IF
           SET ADDRESS OF CVIEW TO WP
           MOVE 7 TO CVIEW
           DISPLAY "CARG=" CARG

           SET WP2 TO ADDRESS OF GARG
           CALL "P1931SUBG" USING GARG RETURNING WP
           IF WP = WP2 DISPLAY "G-SAME" ELSE DISPLAY "G-DIFF" END-IF
           SET ADDRESS OF GVIEW TO WP
           MOVE "WXYZ" TO V1
           DISPLAY "GARG=" GARG

           SET WP2 TO ADDRESS OF TE (2)
           CALL "P1931FWD" USING TE (2) RETURNING WP
           IF WP = WP2 DISPLAY "T-SAME" ELSE DISPLAY "T-DIFF" END-IF
           SET ADDRESS OF TVIEW TO WP
           MOVE "WXYZ" TO TVIEW
           DISPLAY "TBL=" TBL

           SET WP2 TO ADDRESS OF RM (3:4)
           CALL "P1931SUBR" USING RM (3:4) RETURNING WP
           IF WP = WP2 DISPLAY "R-SAME" ELSE DISPLAY "R-DIFF" END-IF
           SET ADDRESS OF TVIEW TO WP
           MOVE "wxyz" TO TVIEW
           DISPLAY "RM=" RM

           INVOKE K1931 "NEW" RETURNING O
           SET U TO O
           MOVE "ORIGINAL" TO ARG
           SET WP2 TO ADDRESS OF ARG
           INVOKE O "MA" USING ARG RETURNING WP
           IF WP = WP2 DISPLAY "M-SAME" ELSE DISPLAY "M-DIFF" END-IF
           SET ADDRESS OF VIEW TO WP
           MOVE "BYMETHOD" TO VIEW
           DISPLAY "ARG=" ARG
           INVOKE U "MA" USING ARG RETURNING WP
           IF WP = WP2 DISPLAY "U-SAME" ELSE DISPLAY "U-DIFF" END-IF
           SET ADDRESS OF VIEW TO WP
           MOVE "UNIVERSL" TO VIEW
           DISPLAY "ARG=" ARG

           MOVE "ORIGINAL" TO ARG
           SET WP TO FUNCTION F1931(ARG)
           IF WP = WP2 DISPLAY "F-SAME" ELSE DISPLAY "F-DIFF" END-IF
           SET ADDRESS OF VIEW TO WP
           MOVE "BYFUNCTN" TO VIEW
           DISPLAY "ARG=" ARG
           STOP RUN.
       END PROGRAM P1931MAIN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FORMAL PIC X(8).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING FORMAL RETURNING RP.
           SET RP TO ADDRESS OF FORMAL
           GOBACK.
       END PROGRAM P1931SUB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931SUBN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FORMAL PIC 9(4).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING FORMAL RETURNING RP.
           SET RP TO ADDRESS OF FORMAL
           GOBACK.
       END PROGRAM P1931SUBN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931SUBC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FORMAL PIC 9(8) COMP.
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING FORMAL RETURNING RP.
           SET RP TO ADDRESS OF FORMAL
           GOBACK.
       END PROGRAM P1931SUBC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931SUBG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FORMAL.
          05 F1 PIC X(4).
          05 F2 PIC 9(4).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING FORMAL RETURNING RP.
           SET RP TO ADDRESS OF FORMAL
           GOBACK.
       END PROGRAM P1931SUBG.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931SUBR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FORMAL PIC X(4).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING FORMAL RETURNING RP.
           SET RP TO ADDRESS OF FORMAL
           GOBACK.
       END PROGRAM P1931SUBR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931FWD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F1 PIC X(4).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING F1 RETURNING RP.
           CALL "P1931LEAF" USING F1 RETURNING RP
           GOBACK.
       END PROGRAM P1931FWD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1931LEAF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC X(4).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING L1 RETURNING RP.
           SET RP TO ADDRESS OF L1
           GOBACK.
       END PROGRAM P1931LEAF.

       IDENTIFICATION DIVISION.
       CLASS-ID. K1931 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(8).
       01 RP USAGE POINTER.
       PROCEDURE DIVISION USING LA RETURNING RP.
           SET RP TO ADDRESS OF LA
           GOBACK.
       END METHOD MA.
       END OBJECT.
       END CLASS K1931.
