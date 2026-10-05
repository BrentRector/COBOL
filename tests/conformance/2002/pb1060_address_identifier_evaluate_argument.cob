      *> kb/Work PB1060 - an ADDRESS-IDENTIFIER (ISO/IEC 1989:2023 8.4.3.1.2
      *> identifier Format 9) is an identifier in two more operand positions:
      *> an EVALUATE selection subject / object, and a function argument.
      *> RULE (14.9.13.3 SR7 a): selection objects "shall be valid operands
      *> for comparison to the corresponding operand in the set of
      *> selection subjects in accordance with 8.8.4.2" - the relation that
      *> compares data-pointers (8.8.4.2.2 Format 3), and 8.4.3.11.4 GR1
      *> makes ADDRESS OF a "unique data item of class pointer".
      *> RULE (8.4.3.2.3 SR8): "Argument-1 shall be an identifier, a
      *> literal, a boolean expression, or an arithmetic expression";
      *> SR10: a BY VALUE formal of a user-defined function takes an
      *> argument "of class numeric, object, or pointer".
      *> RULE (15.14.3 r1 / 15.50.3 r1): BYTE-LENGTH / LENGTH admit "a data
      *> item of any class or category" - the pointer item, 8 bytes in
      *> this implementation (the USAGE POINTER carrier width).
      *> Before the fix the EVALUATE drew COBOLNET2318 "used as a
      *> condition" and the argument COBOLNET0901 "ADDRESS is a reserved
      *> word". The sweep found a WRONG ANSWER beside it: a POINTER subject
      *> read by two WHEN arms is held in an intermediate (14.9.13.4 GR3),
      *> and the copy into it was a MOVE, which stores no pointer - so
      *> EVALUATE P WHEN Q ... WHEN P ... took WHEN OTHER (section D).
      *> EXPECTED OUTPUT, derived line by line (P holds ADDRESS OF B):
      *>   A  ADDRESS OF A against B then A: the second arm matches -> A A
      *>   B  P (= ADDRESS OF B) against ADDRESS OF A, then B        -> B B
      *>   C  ADDRESS OF B ALSO TRUE, WHEN P ALSO (A = A)            -> C BOTH
      *>   D  P against Q (ADDRESS OF A), then P itself              -> D P
      *>   E  BYTE-LENGTH(ADDRESS OF A) = 8, LENGTH(ADDRESS OF A) = 8 -> E 8 8
      *>   F  the function receives ADDRESS OF WX BY VALUE (not NULL),
      *>      bases V-B on it (14.9.39.4 GR13) and returns "AT" followed
      *>      by the two characters there - WX's VALUE "QZ"          -> F ATQZ
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1060F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V-B PIC X(2) BASED.
       LINKAGE SECTION.
       01 L-P USAGE POINTER.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING BY VALUE L-P RETURNING L-R.
           IF L-P = NULL
               MOVE "NULL" TO L-R
           ELSE
               SET ADDRESS OF V-B TO L-P
               MOVE "AT" TO L-R
               MOVE V-B TO L-R(3:2)
           END-IF.
           GOBACK.
       END FUNCTION PB1060F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1060M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1060F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X.
       01 B PIC X.
       01 WX PIC X(2) VALUE "QZ".
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       01 N1 PIC 9.
       01 N2 PIC 9.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF B
           SET Q TO ADDRESS OF A
           EVALUATE ADDRESS OF A
             WHEN ADDRESS OF B DISPLAY "A B"
             WHEN ADDRESS OF A DISPLAY "A A"
             WHEN OTHER DISPLAY "A OTHER"
           END-EVALUATE
           EVALUATE P
             WHEN ADDRESS OF A DISPLAY "B A"
             WHEN ADDRESS OF B DISPLAY "B B"
             WHEN OTHER DISPLAY "B OTHER"
           END-EVALUATE
           EVALUATE ADDRESS OF B ALSO TRUE
             WHEN P ALSO ADDRESS OF A = ADDRESS OF A DISPLAY "C BOTH"
             WHEN OTHER DISPLAY "C OTHER"
           END-EVALUATE
           EVALUATE P
             WHEN Q DISPLAY "D Q"
             WHEN P DISPLAY "D P"
             WHEN OTHER DISPLAY "D OTHER"
           END-EVALUATE
           MOVE FUNCTION BYTE-LENGTH(ADDRESS OF A) TO N1
           MOVE FUNCTION LENGTH(ADDRESS OF A) TO N2
           DISPLAY "E " N1 " " N2
           DISPLAY "F " FUNCTION PB1060F(ADDRESS OF WX)
           STOP RUN.
       END PROGRAM PB1060M.
