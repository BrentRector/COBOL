      *> kb/Work PB1630 - the predefined NULL written as an argument
      *> reaches a formal of class pointer or object reference as that
      *> formal's null value, BY CONTENT and BY VALUE, keyword-less or
      *> not, in all three activations that admit it: the program-
      *> prototype format CALL, the function activation, and INVOKE.
      *> RULE (8.4.3.1.2): NULL is an IDENTIFIER - "Format 8
      *> (predefined-address)" and "Format 6 (predefined-object)".
      *> RULE (8.4.3.10.3 SR1a): "If the class of the associated data
      *> item is pointer, it may be used only as a sending operand in an
      *> INITIALIZE or a SET statement; as an argument in a program-
      *> prototype format CALL statement, a function-prototype format
      *> function activation, or a method invocation".
      *> RULE (14.8.2.3.3): "If the formal parameter is of class pointer
      *> or an object reference described without the ACTIVE-CLASS
      *> phrase, the conformance rules shall be the same as if a SET
      *> statement were performed" - SET pointer TO NULL is valid.
      *> RULE (8.4.3.10.4 GR1): NULL "references a data item of category
      *> data-pointer that contains the null address", so the formal
      *> compares equal to NULL; GR3 is the program-pointer twin, and
      *> 8.4.3.7.4 GR1 gives the object reference "the null object
      *> reference value".
      *> RULE (14.9.4.3 SR22): "identifier-4 shall be of class numeric,
      *> object, or pointer" BY VALUE - NULL is identifier-4, so SR23's
      *> numeric-literal rule does not apply to it.
      *> cite.py --check 8.4.3.1.2 "Format 8 (predefined-address)"
      *>   -> OK  8.4.3.1.2
      *> cite.py --check 8.4.3.10.3 "as an argument in a program-
      *>   prototype format CALL statement, a function-prototype format
      *>   function activation, or a method invocation" -> OK 8.4.3.10.3
      *> cite.py --check 14.8.2.3.3 "If the formal parameter is of class
      *>   pointer or an object reference described without the
      *>   ACTIVE-CLASS phrase, the conformance rules shall be the same
      *>   as if a SET statement were performed" -> OK  14.8.2.3.3
      *> cite.py --check 8.4.3.10.4 "references a data item of category
      *>   data-pointer that contains the null address" -> OK 8.4.3.10.4
      *> cite.py --check 14.9.4.3 "If identifier-4 or its corresponding
      *>   formal parameter is specified with a BY VALUE phrase,
      *>   identifier-4 shall be of class numeric, object, or pointer."
      *>   -> OK  14.9.4.3 22)
      *> Before the fix BY CONTENT NULL compiled and the run died with
      *> EC-PROGRAM-ARG-MISMATCH at the callee's pointer formal, BY VALUE
      *> NULL was refused as a non-numeric literal-2, and INVOKE refused
      *> NULL at a pointer formal as "not yet carried".
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1630F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P USAGE POINTER.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-P RETURNING L-R.
           IF L-P = NULL MOVE "NULL" TO L-R ELSE MOVE "ADDR" TO L-R.
           GOBACK.
       END FUNCTION PB1630F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1630F
           CLASS PB1630K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WP USAGE POINTER.
       01 WX PIC X.
       01 WS PIC X(4).
       01 WO USAGE OBJECT REFERENCE PB1630K.
       PROCEDURE DIVISION.
           SET WP TO ADDRESS OF WX
           CALL "PB1630P" AS NESTED USING BY CONTENT NULL
           CALL "PB1630P" AS NESTED USING BY CONTENT WP
           CALL "PB1630P" AS NESTED USING NULL
           CALL "PB1630V" AS NESTED USING BY VALUE NULL
           CALL "PB1630V" AS NESTED USING NULL
           CALL "PB1630Q" AS NESTED USING BY CONTENT NULL
           CALL "PB1630O" AS NESTED USING BY CONTENT NULL
           MOVE FUNCTION PB1630F(NULL) TO WS
           DISPLAY "F " WS
           MOVE FUNCTION PB1630F(WP) TO WS
           DISPLAY "F " WS
           INVOKE PB1630K "NEW" RETURNING WO
           INVOKE WO "TAKEP" USING BY CONTENT NULL
           INVOKE WO "TAKEP" USING NULL
           INVOKE WO "TAKEO" USING NULL
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "C NULL" ELSE DISPLAY "C ADDR".
           GOBACK.
       END PROGRAM PB1630P.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING BY VALUE LP.
           IF LP = NULL DISPLAY "V NULL" ELSE DISPLAY "V ADDR".
           GOBACK.
       END PROGRAM PB1630V.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING LQ.
           IF LQ = NULL DISPLAY "Q NULL" ELSE DISPLAY "Q ADDR".
           GOBACK.
       END PROGRAM PB1630Q.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1630O.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING LO.
           IF LO = NULL DISPLAY "O NULL" ELSE DISPLAY "O SET".
           GOBACK.
       END PROGRAM PB1630O.
       END PROGRAM PB1630M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1630K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "IP NULL" ELSE DISPLAY "IP ADDR".
           GOBACK.
       END METHOD TAKEP.
       METHOD-ID. TAKEO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING LO.
           IF LO = NULL DISPLAY "IO NULL" ELSE DISPLAY "IO SET".
           GOBACK.
       END METHOD TAKEO.
       END OBJECT.
       END CLASS PB1630K.
