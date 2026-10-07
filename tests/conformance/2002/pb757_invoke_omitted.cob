      *> kb/Work PB757 -- the OMITTED argument and the OPTIONAL formal on the METHOD, CALL and FUNCTION arms
      *> of the one omitted-argument model.  ISO 14.9.23.2 prints the INVOKE USING operand as
      *>     [ BY REFERENCE ] { identifier-3 | OMITTED }
      *> (OMITTED underlined; the brace outside the bracket -- 5.2.6.2 / 5.2.6.3), so both
      *> `USING BY REFERENCE OMITTED` and bare `USING OMITTED` are printed spellings.  Before PB757 both
      *> were COBOL0001 and a method's OPTIONAL formal was COBOLNET0899.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.
      *>   14.9.23.4 GR9: "If an OMITTED phrase is specified or a trailing argument is omitted, the
      *>     omitted-argument condition for that parameter shall be true in the invoked method."
      *>     -> TAKE OMITTED (both spellings); TWO ... B OMITTED (trailing B, 14.8.2.1).
      *>   8.8.4.8.4 GR1 c): true "if the argument corresponding to data-name-1 is itself a formal
      *>     parameter for which the omitted-argument condition is true" -> FWD forwards its omitted F
      *>     to a METHOD (INVOKE SELF) and to a PROGRAM (CALL): TAKE OMITTED / SUB OMITTED; SUB2 forwards
      *>     its own omitted program formal to a METHOD: TAKE OMITTED.
      *>   14.2.3 GR8 (BY REFERENCE): a present argument is the caller's storage, so TAKE's MOVE "WXYZ"
      *>     reaches W4 -> W4=WXYZ.
      *>   8.4.3.2.4 GR7: the same condition in a user-defined FUNCTION, for OMITTED and a trailing
      *>     omission -> OP / PO / PO / PP.
      *>   A universal receiver (14.9.23.4 GR7c) carries the same omission -> TAKE OMITTED, B OMITTED.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB757F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       01 R PIC X(2).
       PROCEDURE DIVISION USING OPTIONAL A OPTIONAL B RETURNING R.
           MOVE "PP" TO R.
           IF A IS OMITTED MOVE "O" TO R(1:1) END-IF.
           IF B IS OMITTED MOVE "O" TO R(2:1) END-IF.
           GOBACK.
       END FUNCTION PB757F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB757M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB757C
           FUNCTION PB757F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB757C.
       01 U USAGE OBJECT REFERENCE.
       01 W4 PIC X(4) VALUE "ABCD".
       01 N PIC 9(3) VALUE 7.
       01 WG.
          05 WG1 PIC X(2) VALUE "GG".
          05 WG2 PIC 9(2) VALUE 42.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB757C "NEW" RETURNING O.
           INVOKE O "TAKE" USING BY REFERENCE OMITTED.
           INVOKE O "TAKE" USING OMITTED.
           INVOKE O "TAKE" USING W4.
           DISPLAY "W4=" W4.
           INVOKE O "TWO" USING N.
           INVOKE O "TWO" USING N W4.
           INVOKE O "FWD" USING OMITTED.
           INVOKE O "FWD" USING W4.
           INVOKE O "GRP" USING OMITTED.
           INVOKE O "GRP" USING WG.
           SET U TO O.
           INVOKE U "TAKE" USING OMITTED.
           INVOKE U "TWO" USING N.
           CALL "PB757S2" USING OMITTED.
           CALL "PB757S2" USING W4.
           DISPLAY "F(OMITTED W4)=" FUNCTION PB757F(OMITTED W4).
           DISPLAY "F(W4 OMITTED)=" FUNCTION PB757F(W4 OMITTED).
           DISPLAY "F(W4)=" FUNCTION PB757F(W4).
           DISPLAY "F(W4 W4)=" FUNCTION PB757F(W4 W4).
           STOP RUN.
       END PROGRAM PB757M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB757S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL P.
           IF P IS OMITTED
               DISPLAY "SUB OMITTED"
           ELSE
               DISPLAY "SUB GIVEN " P
           END-IF.
           GOBACK.
       END PROGRAM PB757S1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB757S2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB757C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O2 USAGE OBJECT REFERENCE PB757C.
       LINKAGE SECTION.
       01 P PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL P.
           INVOKE PB757C "NEW" RETURNING O2.
           DISPLAY "SUB2 FORWARDS".
           INVOKE O2 "TAKE" USING P.
           GOBACK.
       END PROGRAM PB757S2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB757C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-U PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL LK-U.
           IF LK-U IS OMITTED
               DISPLAY "TAKE OMITTED"
           ELSE
               DISPLAY "TAKE GIVEN " LK-U
               MOVE "WXYZ" TO LK-U
           END-IF.
       END METHOD TAKE.

       METHOD-ID. TWO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9(3).
       01 B PIC X(4).
       PROCEDURE DIVISION USING A OPTIONAL B.
           IF B IS NOT OMITTED
               DISPLAY "TWO A=" A " B=" B
           ELSE
               DISPLAY "TWO A=" A " B OMITTED"
           END-IF.
       END METHOD TWO.

       METHOD-ID. FWD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL F.
           INVOKE SELF "TAKE" USING F.
           CALL "PB757S1" USING F.
       END METHOD FWD.

       METHOD-ID. GRP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       PROCEDURE DIVISION USING OPTIONAL G.
           IF G IS OMITTED
               DISPLAY "GRP OMITTED"
           ELSE
               DISPLAY "GRP GIVEN " G1 " " G2
           END-IF.
       END METHOD GRP.
       END OBJECT.
       END CLASS PB757C.
