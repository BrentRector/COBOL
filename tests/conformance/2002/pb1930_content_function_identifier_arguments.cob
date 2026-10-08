      *> kb/Work PB1930. ISO 14.9.4.4 GR8: "An argument that consists merely of a single identifier or literal is
      *> regarded as an identifier or literal rather than an arithmetic or boolean expression." A function-identifier
      *> is an identifier (8.4.3.1.2 Format 1; the word FUNCTION may be omitted for a repository function, 8.4.3.2.3
      *> SR2), and it references "the unique data item that results from the evaluation of a function" (8.4.3.2.1),
      *> a temporary of the RETURNING item's class and category. The CALL binder bound it as arithmetic-expression-1
      *> and 8.8.1.1 refused every function that does not return a number ("not a numeric operand", COBOLNET0844) --
      *> an object reference, a pointer, and even an alphanumeric intrinsic. It is a sending temporary instead:
      *>   BY CONTENT / keyword-less in Format 2 (14.9.4.4 GR9 a)2: it is in no file, working-storage, local-storage
      *>   or linkage section, so SR3 is not met and BY CONTENT is assumed), BY VALUE (SR22: numeric, object and
      *>   pointer identifiers), and the INVOKE twin (14.9.23.4 GR6 a)2).
      *> EXPECTED OUTPUT, derived line by line:
      *>   1  CALL AS NESTED ... BY CONTENT FUNCTION MK(1): MK returns a fresh account, whose WHO is "ACCOUNT" in
      *>      PIC X(8) (14.9.25.4: left-justified, space-filled)                                   "SO=ACCOUNT "
      *>   2  the keyword-less Format 2 spelling FUNCTION MK(2): GR9 a)2 assumes BY CONTENT            "SO=ACCOUNT "
      *>   3  the same with the word FUNCTION omitted, MK(3) (8.4.3.2.3 SR2)                          "SO=ACCOUNT "
      *>   4  BY CONTENT FUNCTION PT(1): PT ALLOCATEs and returns the address, so it is not NULL
      *>      (14.9.3.4 GR1; GR2 yields NULL only for a size of 0 or less)                                   "SP=SET"
      *>   5  BY CONTENT FUNCTION N(41): N returns its argument plus 1 into PIC 9(4)                      "SN=0042"
      *>   6  BY VALUE N(1) into a BY VALUE formal: 1 + 1                                                 "SNV=0002"
      *>   7  BY VALUE FUNCTION PT(1) into a BY VALUE pointer formal                                        "SPV=SET"
      *>   8  Format 1 CALL "..." USING BY CONTENT FUNCTION UPPER-CASE(X), X = "abc": identifier-2          "SA=ABC"
      *>   9  INVOKE O "TAKEO" USING BY CONTENT FUNCTION MK(1) into an object-reference formal    "TAKEO=ACCOUNT "
      *>  10  INVOKE O "TAKEP" USING BY CONTENT FUNCTION PT(1) into a pointer formal                    "TAKEP=SET"
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1930MK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1930ACC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-OBJ USAGE OBJECT REFERENCE PB1930ACC.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1930ACC "NEW" RETURNING L-OBJ
           GOBACK.
       END FUNCTION PB1930MK.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1930PT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES USAGE POINTER.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           ALLOCATE 4 CHARACTERS RETURNING L-RES
           GOBACK.
       END FUNCTION PB1930PT.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1930N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES PIC 9(4).
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           COMPUTE L-RES = L-SEED + 1
           GOBACK.
       END FUNCTION PB1930N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1930ACC
           FUNCTION PB1930MK
           FUNCTION PB1930PT
           FUNCTION PB1930N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       01 O USAGE OBJECT REFERENCE PB1930ACC.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1930SO" AS NESTED
               USING BY CONTENT FUNCTION PB1930MK(1)
           CALL "PB1930SO" AS NESTED USING FUNCTION PB1930MK(2)
           CALL "PB1930SO" AS NESTED USING PB1930MK(3)
           CALL "PB1930SP" AS NESTED
               USING BY CONTENT FUNCTION PB1930PT(1)
           CALL "PB1930SN" AS NESTED
               USING BY CONTENT FUNCTION PB1930N(41)
           CALL "PB1930SV" AS NESTED USING BY VALUE FUNCTION PB1930N(1)
           CALL "PB1930SW" AS NESTED USING BY VALUE FUNCTION PB1930PT(1)
           CALL "PB1930SA" USING BY CONTENT FUNCTION UPPER-CASE(X)
           INVOKE PB1930ACC "NEW" RETURNING O
           INVOKE O "TAKEO" USING BY CONTENT FUNCTION PB1930MK(1)
           INVOKE O "TAKEP" USING BY CONTENT FUNCTION PB1930PT(1)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE OBJECT REFERENCE PB1930ACC.
       PROCEDURE DIVISION USING L.
           DISPLAY "SO=" L :: "WHO"
           GOBACK.
       END PROGRAM PB1930SO.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE POINTER.
       PROCEDURE DIVISION USING L.
           IF L = NULL
               DISPLAY "SP=NULL"
           ELSE
               DISPLAY "SP=SET"
           END-IF
           GOBACK.
       END PROGRAM PB1930SP.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "SN=" L
           GOBACK.
       END PROGRAM PB1930SN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L.
           DISPLAY "SNV=" L
           GOBACK.
       END PROGRAM PB1930SV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE POINTER.
       PROCEDURE DIVISION USING BY VALUE L.
           IF L = NULL
               DISPLAY "SPV=NULL"
           ELSE
               DISPLAY "SPV=SET"
           END-IF
           GOBACK.
       END PROGRAM PB1930SW.
       END PROGRAM PB1930M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1930SA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION USING L.
           DISPLAY "SA=" L
           GOBACK.
       END PROGRAM PB1930SA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1930ACC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD WHO.
       METHOD-ID. TAKEO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE OBJECT REFERENCE PB1930ACC.
       PROCEDURE DIVISION USING L.
           DISPLAY "TAKEO=" L :: "WHO".
       END METHOD TAKEO.
       METHOD-ID. TAKEP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE POINTER.
       PROCEDURE DIVISION USING L.
           IF L = NULL
               DISPLAY "TAKEP=NULL"
           ELSE
               DISPLAY "TAKEP=SET"
           END-IF.
       END METHOD TAKEP.
       END OBJECT.
       END CLASS PB1930ACC.
