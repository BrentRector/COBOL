      *> kb/Work PB1430 - the conforming side of two rules this compiler
      *> now enforces, so the enforcement can never reject legal source:
      *>  ISO 8.7.4 / 8.7.3: the invocation operator '::' and the
      *>   concatenation operator '&' "shall be immediately preceded and
      *>   followed by a separator space" - and 6.4.2 says "The last
      *>   nonblank character of each line is treated as if it were
      *>   followed by a space", so an operator at either end of a line
      *>   is conforming;
      *>  ISO 8.4.3.4.2: a written parenthesis pair holds one or more
      *>   arguments (the brace repeats inside the one pair), separated by
      *>   separator spaces or separator commas.
      *> Every expected value is DERIVED from the standard, not captured:
      *>   1, 2  8.4.3.4.4 GR1 b): the temporary is described like the
      *>         method's RETURNING item PIC X(7) "ACCOUNT"; MOVE to
      *>         PIC X(8) left-justifies and space-fills (14.9.25.4) ->
      *>         "ACCOUNT ".
      *>   3     GR1 a): (3 4) are the USING arguments, 3 + 4 = 7 -> 0007.
      *>   4     (5, 6), the separator comma form: 5 + 6 = 11 -> 0011.
      *>   5     8.8.3.3 GR2: the value is the concatenation of the
      *>         operands, "AB" & "C" & "D" -> "ABCD".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1430SP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1430SPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1430SPC.
       01 W  PIC X(8).
       01 W4 PIC X(4).
       01 N  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1430SPC "NEW" RETURNING A1.
           MOVE A1
               :: "GETNAME" TO W.
           DISPLAY "1=" W.
           MOVE SPACES TO W.
           MOVE A1 ::
               "GETNAME" TO W.
           DISPLAY "2=" W.
           MOVE A1 :: "SUM2" (3 4) TO N.
           DISPLAY "3=" N.
           MOVE A1 :: "SUM2" (5, 6) TO N.
           DISPLAY "4=" N.
           MOVE "AB"
               & "C" &
               "D" TO W4.
           DISPLAY "5=" W4.
           STOP RUN.
       END PROGRAM PB1430SP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1430SPC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(7).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       METHOD-ID. SUM2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A   PIC 9(4).
       01 LK-B   PIC 9(4).
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION USING LK-A LK-B RETURNING LK-RES.
       MAIN.
           ADD LK-A LK-B GIVING LK-RES.
       END METHOD SUM2.
       END OBJECT.
       END CLASS PB1430SPC.
