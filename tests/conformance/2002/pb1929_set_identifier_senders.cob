      *> kb/Work PB1929 — every sending brace of ISO/IEC 1989:2023 §14.9.39.2's To-formats names an IDENTIFIER, and
      *> §8.4.3.1.3 SR1 makes that "any of the formats for an identifier". A function-identifier (§8.4.3.1.2 Format 1),
      *> an inline method invocation (Format 4) and the keyword-omitted function (§8.4.3.2.3 SR2) are three of them,
      *> and each references a DATA ITEM — §8.4.3.2.1 "A function-identifier references the unique data item that
      *> results from the evaluation of a function"; §8.4.3.4.4 GR1 "An inline method invocation references a
      *> temporary data item with the same class, category, and content as the temp-identifier". So the sender's class
      *> and category are the RETURNING item's, and each format's sending rule is satisfied by it:
      *>   Format 5  §14.9.39.3 SR9   "Identifier-4 shall be an object reference"
      *>   Format 7  §14.9.39.3 SR17  "Identifier-6 shall be of category data-pointer"
      *>   Format 9  §14.9.39.3 SR21  identifier-8 of category program-pointer
      *>   Format 1  §14.9.39.3 SR2   "Identifier-2 shall reference a data item of class index"
      *> The bare-reference sniff (OoExtractBareReference) answered null for all three shapes, so each format saw "no
      *> identifier" and refused the sender as a literal or an arithmetic expression (COBOLNET0867 / 0869).
      *> (Format 8, the function-pointer twin, is 2014 and has its own golden:
      *> 2014/pb1929_set_function_pointer_function_sender.)
      *> EXPECTED OUTPUT, derived line by line:
      *>   1  SET A2 TO FUNCTION PB1929MK(1): the function's temporary is an object reference to a fresh account;
      *>      its WHO method returns "ACCOUNT" in PIC X(8), so "ACCOUNT " (§14.9.25.4, Table 16: alphanumeric to alphanumeric is left-justified and space-filled)
      *>   2  the keyword-omitted spelling PB1929MK(2) is the same function-identifier (§8.4.3.2.3 SR2)        "ACCOUNT "
      *>   3  SET A4 TO F1 :: "MAKE": an inline method invocation, whose temporary is the invoked method's RETURNING
      *>      item (§8.4.3.4.4 GR1 b)); MAKE returns a fresh account                                          "ACCOUNT "
      *>   4  SET DP TO FUNCTION PB1929PT(1): the function ALLOCATEs and returns the address, so the pointer is not
      *>      NULL (§14.9.3.4 GR1 allocates the bytes; GR2 yields NULL only for a size of 0 or less)                           "SET"
      *>   5  SET PP TO FUNCTION PB1929PG(1): the function returns the program address of PB1929SUB (§8.4.3.13.4
      *>      GR1 b)), so CALL PP activates it and it displays                                                  IN-SUB
      *>   6  SET TX TO FUNCTION PB1929IX(1): the function returns an INDEX data item holding occurrence 4 (class
      *>      index, so identifier-2 — §14.9.39.3 SR2); positioning TX at it selects "ABCDEFGHI"(4)            "D"
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929MK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1929ACC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-OBJ USAGE OBJECT REFERENCE PB1929ACC.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1929ACC "NEW" RETURNING L-OBJ
           GOBACK.
       END FUNCTION PB1929MK.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929PT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES USAGE POINTER.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           ALLOCATE 4 CHARACTERS RETURNING L-RES
           GOBACK.
       END FUNCTION PB1929PT.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929PG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           SET L-RES TO ADDRESS OF PROGRAM "PB1929SUB"
           GOBACK.
       END FUNCTION PB1929PG.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1929IX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FT.
          05 FE PIC X OCCURS 9 TIMES INDEXED BY FX.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-RES USAGE INDEX.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           SET FX TO 4
           SET L-RES TO FX
           GOBACK.
       END FUNCTION PB1929IX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1929M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1929ACC
           CLASS PB1929FAC
           FUNCTION PB1929MK
           FUNCTION PB1929PT
           FUNCTION PB1929PG
           FUNCTION PB1929IX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE OBJECT REFERENCE PB1929FAC.
       01 A2 USAGE OBJECT REFERENCE PB1929ACC.
       01 A3 USAGE OBJECT REFERENCE PB1929ACC.
       01 A4 USAGE OBJECT REFERENCE PB1929ACC.
       01 W  PIC X(8).
       01 DP USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 T.
          05 TE PIC X OCCURS 9 TIMES INDEXED BY TX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDEFGHI" TO T
           INVOKE PB1929FAC "NEW" RETURNING F1
           SET A2 TO FUNCTION PB1929MK(1)
           MOVE A2 :: "WHO" TO W
           DISPLAY "1=" W
           SET A3 TO PB1929MK(2)
           MOVE A3 :: "WHO" TO W
           DISPLAY "2=" W
           SET A4 TO F1 :: "MAKE"
           MOVE A4 :: "WHO" TO W
           DISPLAY "3=" W
           SET DP TO FUNCTION PB1929PT(1)
           IF DP = NULL
               DISPLAY "4=NULL"
           ELSE
               DISPLAY "4=SET"
           END-IF
           FREE DP
           SET PP TO FUNCTION PB1929PG(1)
           CALL PP
           SET TX TO FUNCTION PB1929IX(1)
           DISPLAY "6=" TE(TX)
           STOP RUN.
       END PROGRAM PB1929M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1929ACC INHERITS FROM BASE.
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
       END OBJECT.
       END CLASS PB1929ACC.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1929FAC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1929ACC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-OBJ USAGE OBJECT REFERENCE PB1929ACC.
       PROCEDURE DIVISION RETURNING LK-OBJ.
       MAIN.
           INVOKE PB1929ACC "NEW" RETURNING LK-OBJ.
       END METHOD MAKE.
       END OBJECT.
       END CLASS PB1929FAC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1929SUB.
       PROCEDURE DIVISION.
           DISPLAY "IN-SUB"
           GOBACK.
       END PROGRAM PB1929SUB.
