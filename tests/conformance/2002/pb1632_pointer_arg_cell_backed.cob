      *> kb/Work PB1632 - a class-pointer argument whose storage is
      *> cell-backed (its ADDRESS OF taken, or BASED) crosses CALL in
      *> its own carrier. ISO 14.8.2.3.3 2): a pointer formal conforms
      *> as if a SET statement were performed; 14.2.3 GR9 makes BY
      *> CONTENT a copy and GR8 makes BY REFERENCE the same storage.
      *> Before the fix every BY CONTENT arm here crashed the compiler
      *> ("no character image for category Pointer").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WP USAGE POINTER.
       01 WQ USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 BP USAGE POINTER BASED.
       PROCEDURE DIVISION.
      *>   a pointer holding its own address, BY CONTENT
           SET WP TO ADDRESS OF WP
           SET WQ TO ADDRESS OF WP
           CALL "PB1632C" AS NESTED USING BY CONTENT WP WQ
      *>   BY CONTENT is a copy: the callee's SET does not reach WP
           CALL "PB1632N" AS NESTED USING BY CONTENT WP
           IF WP = WQ DISPLAY "CONTENT-KEPT" ELSE DISPLAY "CONTENT-LOST"
           END-IF
      *>   BY REFERENCE is the same storage: the callee's SET does
           CALL "PB1632N" AS NESTED USING BY REFERENCE WP
           IF WP = NULL DISPLAY "REF-NULLED" ELSE DISPLAY "REF-KEPT"
           END-IF
      *>   a program-pointer whose address is taken, BY CONTENT
           SET PP TO ADDRESS OF PROGRAM "PB1632X"
           SET WQ TO ADDRESS OF PP
           CALL "PB1632P" AS NESTED USING BY CONTENT PP
      *>   a BASED pointer, BY CONTENT then BY REFERENCE
           ALLOCATE BP
           SET BP TO ADDRESS OF WQ
           CALL "PB1632T" AS NESTED USING BY CONTENT BP
           CALL "PB1632N" AS NESTED USING BY REFERENCE BP
           IF BP = NULL DISPLAY "BASED-REF-NULLED"
           ELSE DISPLAY "BASED-REF-KEPT"
           END-IF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       01 LQ USAGE POINTER.
       PROCEDURE DIVISION USING LP LQ.
           IF LP = LQ DISPLAY "CONTENT-SAME" ELSE DISPLAY "CONTENT-DIFF".
           GOBACK.
       END PROGRAM PB1632C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           SET LP TO NULL.
           GOBACK.
       END PROGRAM PB1632N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "BASED-NULL" ELSE DISPLAY "BASED-ADDR".
           GOBACK.
       END PROGRAM PB1632T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LPP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING LPP.
           IF LPP = NULL DISPLAY "PP-NULL" ELSE DISPLAY "PP-SET".
           GOBACK.
       END PROGRAM PB1632P.
       END PROGRAM PB1632G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1632X.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB1632X.
