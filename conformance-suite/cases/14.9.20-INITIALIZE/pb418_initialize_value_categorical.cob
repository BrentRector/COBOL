*>--- conformance-case
*> id: pb418_initialize_value_categorical
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418CAT.
      *> kb/Work PB418 — ISO 14.9.20.4 GR5c1a, the CATEGORICAL alternative of the VALUE phrase's qualification
      *> test, at the introduction edition of every construct it needs (COBOL-2002).
      *>
      *> GR5c1: "The VALUE phrase is specified, the category of the elementary data item is one of the categories
      *> specified or implied in the VALUE phrase, and one of the following is true:
      *>    a. Either the category of the elementary data item is data-pointer, message-tag, object-reference,
      *>       or program-pointer, or
      *>    b. A data-item format VALUE clause is specified in the data description entry of the elementary
      *>       data item."
      *> GR2 makes ALL "as if all of the categories listed in category-name were specified".
      *> GR6a1: "If the category of the receiving-operand is data-pointer, function-pointer, message-tag, or
      *> program-pointer, the sending-operand is the predefined address NULL"; GR6a2: "If the category of the
      *> receiving-operand is object-reference, the sending-operand is the predefined object reference NULL".
      *> GR4: for those categories "the implicit statement is: SET receiving-operand TO sending-operand".
      *>
      *> WHY GR5c1a EXISTS, and why it cannot be replaced by GR5c1b: these categories can never carry a VALUE
      *> clause. 13.18.63.3 SR9 - "The VALUE clause shall not be specified if a USAGE clause with a phrase of
      *> FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER is also specified" - and for a plain
      *> USAGE POINTER the only writable value is the predefined address NULL, which 8.4.3.10.3 SR1a permits
      *> "only as a sending operand in an INITIALIZE or a SET statement", as an argument, or in a relation
      *> condition. Reading GR5c1b as the whole premise therefore left this arm 100% dead and a pointer silently
      *> kept its old address across INITIALIZE ... ALL TO VALUE.
      *>
      *> EXPECTED, DERIVED FROM THE RULES ABOVE AND WRITTEN DOWN BEFORE THE RUN:
      *>   1[NULL]              P1 is SET to an address, then GR5c1a qualifies it and GR6a1 nulls it.
      *>   2[NULL]              the same for USAGE PROGRAM-POINTER (GR5c1a + GR6a1).
      *>   3[NULL]              the same for USAGE OBJECT REFERENCE (GR5c1a + GR6a2).
      *>   4[NULL|QQQ|VVV]      inside a group: the pointer member is nulled by GR5c1a/GR6a1; TP-X carries NO
      *>                        VALUE clause and is category alphanumeric, so GR5c1a is false, GR5c1b and GR5c1c
      *>                        are false, and GR5c2/c3/c4 are all false under a bare ALL TO VALUE - it is NOT a
      *>                        receiving-operand and keeps QQQ; TP-V has a data-item format VALUE clause, so
      *>                        GR5c1b qualifies it and GR6a3 restores VVV.
      *>   5[NULL]              CONTROL - the bare COBOL-85 form reaches the same receiver through GR5c4 and
      *>                        GR6c's "Data-pointer | Predefined address NULL" row, proving the SET-to-NULL
      *>                        machinery was live before line 1 accused the VALUE arm.
      *>   6[SET]               CONTROL IN THE OPPOSITE DIRECTION - REPLACING ALPHANUMERIC alone names a category
      *>                        the pointer is not, so GR5c2 fails, GR5c1 fails (no VALUE phrase) and GR5c4 fails
      *>                        (a REPLACING phrase IS specified): the pointer keeps its address.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB418CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TGT-A PIC X(4) VALUE "ABCD".
       01 P1 USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 O1 USAGE OBJECT REFERENCE PB418CL.
      *> 13.18.60.3 SR14 lets a USAGE POINTER item stand below level 01 only under a STRONG type declaration.
       01 PB418T IS TYPEDEF STRONG.
           05 TP-P USAGE POINTER.
           05 TP-X PIC X(3).
           05 TP-V PIC X(3) VALUE "VVV".
       01 GV TYPE PB418T.
       PROCEDURE DIVISION.
       MAIN.
           SET P1 TO ADDRESS OF TGT-A
           INITIALIZE P1 ALL TO VALUE
           IF P1 = NULL THEN DISPLAY "1[NULL]" ELSE DISPLAY "1[SET]" END-IF

           SET PP TO ENTRY "PB418SUB"
           INITIALIZE PP ALL TO VALUE
           IF PP = NULL THEN DISPLAY "2[NULL]" ELSE DISPLAY "2[SET]" END-IF

           INVOKE PB418CL "NEW" RETURNING O1
           INITIALIZE O1 ALL TO VALUE
           IF O1 = NULL THEN DISPLAY "3[NULL]" ELSE DISPLAY "3[SET]" END-IF

           SET TP-P OF GV TO ADDRESS OF TGT-A
           MOVE "QQQ" TO TP-X OF GV
           MOVE "WWW" TO TP-V OF GV
           INITIALIZE GV ALL TO VALUE
           IF TP-P OF GV = NULL
               THEN DISPLAY "4[NULL|" TP-X OF GV "|" TP-V OF GV "]"
               ELSE DISPLAY "4[SET|" TP-X OF GV "|" TP-V OF GV "]"
           END-IF

           SET P1 TO ADDRESS OF TGT-A
           INITIALIZE P1
           IF P1 = NULL THEN DISPLAY "5[NULL]" ELSE DISPLAY "5[SET]" END-IF

           SET P1 TO ADDRESS OF TGT-A
           INITIALIZE P1 REPLACING ALPHANUMERIC DATA BY "ZZZZ"
           IF P1 = NULL THEN DISPLAY "6[NULL]" ELSE DISPLAY "6[SET]" END-IF
           STOP RUN.
       END PROGRAM PB418CAT.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418SUB.
       PROCEDURE DIVISION.
       S-MAIN.
           CONTINUE.
       END PROGRAM PB418SUB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB418CL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS PB418CL.
