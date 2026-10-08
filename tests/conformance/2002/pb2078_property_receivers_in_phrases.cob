      *> kb/Work PB2078, the statements beyond the arithmetic ones and MOVE. ISO 14.9.25.4 GR1: "Item identification for
      *> identifier-2 is performed immediately before the data is moved to the respective data item", and 14.7.7 4) b)
      *> says the same of an arithmetic statement's receivers. The statements below store a receiver through a MOVE
      *> they define (READ ... INTO: 14.9.30.4 GR4 b), UNSTRING INTO) or in one read-and-store of their own (STRING INTO,
      *> INSPECT REPLACING / CONVERTING, INITIALIZE), and an object property is its data item's stand-in (8.4.3.9.4):
      *> its GET and SET belong around THAT access. The accessors used to be placed around the whole statement, so
      *> BAL / NM OF AR(I) was re-identified after the statement's own ON OVERFLOW / NOT AT END phrase had changed I
      *> -- the SET reached AR(2) -- and the shape was refused (COBOLNET0899).
      *> EXPECTED, derived line by line (AR(1) and AR(2) start as "ABCDEF"; NM is PIC X(6)):
      *>   1  READ F INTO NM OF AR(I), I = 1, record "77777": the record moves to AR(1)'s NM, left-justified and
      *>      space-filled (14.9.25.4 GR6); NOT AT END then adds 1 to I                "READ I=2 [77777 ]/[ABCDEF]"
      *>   2  STRING "UVWXYZ123" ... INTO NM OF AR(1): 6 characters fit (14.9.43.4 GR6), the rest overflows (GR8) and
      *>      ON OVERFLOW adds 1 to I                                                "STRING I=2 [UVWXYZ]/[ABCDEF]"
      *>   3  INSPECT NM OF AR(1) REPLACING ALL "U" BY "Q": "QVWXYZ"; then I = 2 and CONVERTING "ABC" TO "xyz" on
      *>      AR(2): "xyzDEF"                                                         "INSPECT [QVWXYZ]/[xyzDEF]"
      *>   4  UNSTRING "AB,CD" DELIMITED BY "," INTO NM OF AR(1): "AB" space-filled to 6; "CD" stays unexamined with
      *>      every receiving area acted upon, so ON OVERFLOW (14.9.48.4 GR15 b) adds 1 to I
      *>                                                                              "UNSTRING I=2 [AB    ]/[xyzDEF]"
      *>   5  INITIALIZE NM OF AR(1): alphanumeric to SPACES (14.9.20.4 GR6)         "INIT [      ]/[xyzDEF]"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2078P2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2078VC
           PROPERTY NM.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2078p2.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(5).
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB2078VC OCCURS 2.
       01 R TYPE RT.
       01 I PIC 9.
       01 T PIC X(5) VALUE "AB,CD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2078VC "NEW" RETURNING AR(1)
           INVOKE PB2078VC "NEW" RETURNING AR(2)
           OPEN OUTPUT F
           MOVE "77777" TO FR
           WRITE FR
           CLOSE F
           OPEN INPUT F
           MOVE 1 TO I
           READ F INTO NM OF AR(I)
               NOT AT END ADD 1 TO I
           END-READ
           CLOSE F
           DISPLAY "READ I=" I " [" NM OF AR(1) "]/[" NM OF AR(2) "]"
           MOVE 1 TO I
           STRING "UVWXYZ123" DELIMITED BY SIZE INTO NM OF AR(I)
               ON OVERFLOW ADD 1 TO I
           END-STRING
           DISPLAY "STRING I=" I " [" NM OF AR(1) "]/[" NM OF AR(2) "]"
           MOVE 1 TO I
           INSPECT NM OF AR(I) REPLACING ALL "U" BY "Q"
           MOVE 2 TO I
           INSPECT NM OF AR(I) CONVERTING "ABC" TO "xyz"
           DISPLAY "INSPECT [" NM OF AR(1) "]/[" NM OF AR(2) "]"
           MOVE 1 TO I
           UNSTRING T DELIMITED BY "," INTO NM OF AR(I)
               ON OVERFLOW ADD 1 TO I
           END-UNSTRING
           DISPLAY "UNSTRING I=" I " [" NM OF AR(1) "]/[" NM OF AR(2) "]"
           MOVE 1 TO I
           INITIALIZE NM OF AR(I)
           DISPLAY "INIT [" NM OF AR(1) "]/[" NM OF AR(2) "]"
           STOP RUN.
       END PROGRAM PB2078P2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2078VC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB2078VC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB2078VC.
