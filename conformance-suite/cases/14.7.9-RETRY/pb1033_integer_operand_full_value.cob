*>--- conformance-case
*> id: pb1033_integer_operand_full_value
*> rules: [GR-14.7.9.3-1, GR-14.7.9.3-2, GR-14.9.17.4-2, GR-14.9.43.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> AN INTEGER OPERAND IS CARRIED AT ITS FULL VALUE (kb/Work PB1033).
      *> An integer data item or literal holds up to 31 digits, and every
      *> consumer below used to narrow it with a C# cast that WRAPS: a value
      *> of 2**32 + 1 = 4294967297 became 1, 2**64 + 3 became 3. Each rule
      *> asks about the VALUE, so each wrapped value took a wrong answer.
      *> - GO TO ... DEPENDING, ISO §14.9.17.4 GR2: control goes to
      *>   procedure-name-1, etc., "depending on the value of identifier-1
      *>   being 1, 2, ..., n"; any other value: "no transfer occurs". So
      *>   G = 4294967297 FALLS THROUGH (the wrap went to P1) and G = 2 goes
      *>   to P2.
      *> - Reference modification, §8.4.3.3.4 GR5 c): "a value that
      *>   references a position outside the area of identifier-1" raises
      *>   EC-BOUND-REF-MOD. X is 5 positions: leftmost 4294967297 (the wrap
      *>   read position 1), length 4294967297, and leftmost 2 with length
      *>   2147483647 (its end position 2147483648 overflowed a 32-bit sum
      *>   and passed the test) all raise it; the declarative catches each.
      *> - PERFORM TIMES, §14.9.28.4 GR9: "performed the number of times
      *>   specified by integer-1 or by the value of the data item". A
      *>   20-digit literal (a backend CS1021 before) and C = 2**64 + 1 (the
      *>   wrap performed ONCE) both run past 3 iterations; EXIT PERFORM ends
      *>   each at 3.
      *> - STRING POINTER, §14.9.43.4 GR8: before each move, a pointer that
      *>   "exceeds the number of character positions" of identifier-3 is an
      *>   overflow; GR6: the pointer "is changed ... only by the behavior
      *>   specified above". P = 2**64 + 3 (the wrap wrote at position 3 and
      *>   stored P = 5) takes ON OVERFLOW, leaves OUT1 and P unchanged.
      *> - UNSTRING, §14.9.48.4 GR15 a): a pointer "greater than the number
      *>   of character positions" of identifier-1 is an overflow and GR16 a)
      *>   terminates the statement, so Q = 2**64 + 3 is unchanged. GR14: the
      *>   TALLYING item ends at "its value at the beginning of the execution
      *>   ... plus a value equal to the number of data receiving items acted
      *>   upon" — 18446744073709551610 + 1, carried exactly; the pointer
      *>   stops after the first delimiter, at 4 (GR13).
      *> - WRITE ADVANCING 4294967297 LINES and RETRY 4294967297 TIMES / FOR
      *>   4294967297 SECONDS are legal literals that did not COMPILE (CS0221);
      *>   they sit in a branch that never runs, so compiling is the check.
      >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1033FV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F ASSIGN TO "pb1033-never-opened.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(3).
       WORKING-STORAGE SECTION.
       01 G    PIC 9(10) VALUE 4294967297.
       01 X    PIC X(5)  VALUE "ABCDE".
       01 S    PIC 9(10) VALUE 4294967297.
       01 L    PIC 9(10) VALUE 2147483647.
       01 C    PIC 9(20) VALUE 18446744073709551617.
       01 N    PIC 9     VALUE 0.
       01 OUT1 PIC X(10) VALUE ALL "-".
       01 P    PIC 9(20) VALUE 18446744073709551619.
       01 Q    PIC 9(20) VALUE 18446744073709551619.
       01 T    PIC 9(20) VALUE 18446744073709551610.
       01 SRC  PIC X(6)  VALUE "AB,CD,".
       01 R1   PIC X(3).
       01 NEVER PIC 9    VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RM-CHECK SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       RM-CHECK-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           GO TO P1 P2 DEPENDING ON G.
           DISPLAY "GOTO-FELL-THROUGH".
           MOVE 2 TO G.
           GO TO P1 P2 DEPENDING ON G.
           DISPLAY "GOTO-WRONG".
       P1.
           DISPLAY "GOTO-P1-WRONG".
           STOP RUN.
       P2.
           DISPLAY "GOTO-P2".
           DISPLAY "START=[" X(S:1) "]".
           DISPLAY "LENGTH=[" X(1:S) "]".
           DISPLAY "END=[" X(2:L) "]".
           PERFORM 99999999999999999999 TIMES
               ADD 1 TO N
               IF N = 3 EXIT PERFORM END-IF
           END-PERFORM.
           DISPLAY "LITERAL-TIMES=" N.
           MOVE 0 TO N.
           PERFORM C TIMES
               ADD 1 TO N
               IF N = 3 EXIT PERFORM END-IF
           END-PERFORM.
           DISPLAY "ITEM-TIMES=" N.
           STRING "AB" DELIMITED BY SIZE INTO OUT1 WITH POINTER P
               ON OVERFLOW DISPLAY "STRING-OVERFLOW"
               NOT ON OVERFLOW DISPLAY "STRING-NO-OVERFLOW"
           END-STRING.
           DISPLAY "OUT1=[" OUT1 "] P=" P.
           UNSTRING SRC DELIMITED BY "," INTO R1 WITH POINTER Q
               ON OVERFLOW DISPLAY "UNSTRING-OVERFLOW"
           END-UNSTRING.
           DISPLAY "Q=" Q.
           MOVE 1 TO Q.
           UNSTRING SRC DELIMITED BY "," INTO R1 WITH POINTER Q
               TALLYING IN T
           END-UNSTRING.
           DISPLAY "R1=[" R1 "] Q=" Q " T=" T.
           IF NEVER = 1
               OPEN INPUT RETRY 4294967297 TIMES F
               OPEN INPUT RETRY FOR 4294967297 SECONDS F
               WRITE R AFTER ADVANCING 4294967297 LINES
           END-IF.
           STOP RUN.
