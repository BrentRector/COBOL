*>--- conformance-case
*> id: pb1021_address_identifier_operands
*> rules: [SR-8.4.3.1.3-10, SR-14.9.23.3-9, SR-14.9.23.3-19]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1021 - an ADDRESS-IDENTIFIER as a relation operand and
      *> as an INVOKE argument (the twins of PB239's CALL argument).
      *> 8.4.3.1.2 identifier Format 9 makes ADDRESS OF identifier-1
      *> (8.4.3.11) and ADDRESS OF PROGRAM (8.4.3.13) identifiers, and
      *> 8.4.3.11.4 GR1 / 8.4.3.13.4 GR1 make each "a unique data item of
      *> class pointer". So 8.8.4.2.2 Format 3 (identifier-3 = identifier-4)
      *> admits it, and 14.9.23.3 SR9: "Identifier-3 shall be an
      *> address-identifier or shall reference a data item defined in the
      *> file, working-storage, local-storage, or linkage section"; SR19:
      *> "If identifier-3 references an address-identifier, identifier-3 is
      *> a sending operand". Before PB1021 every relation and INVOKE below
      *> was a COBOL0001 parse error.
      *> Expected values: 8.8.4.2.16 "The operands are equal if they
      *> reference the same address" - so ADDRESS OF X equals a pointer SET
      *> to ADDRESS OF X and differs from ADDRESS OF Y; the method returns
      *> the pointer value it received, so each phrase (none, BY REFERENCE,
      *> BY CONTENT, the 8.4.3.4 inline form) must hand back ADDRESS OF the
      *> argument's item, and the method's SET LP TO NULL cannot reach the
      *> caller (SR19 - a sending operand). ADDRESS OF PROGRAM names this
      *> program, which is locatable, so it is not NULL (8.4.3.13.4 GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1021AI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1021K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 Y PIC X(4) VALUE "WXYZ".
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 R PIC X(4).
       01 O USAGE OBJECT REFERENCE PB1021K.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X.
           IF P = ADDRESS OF X DISPLAY "REL-1 EQ" ELSE DISPLAY "REL-1 NE".
           IF ADDRESS OF X = P DISPLAY "REL-2 EQ" ELSE DISPLAY "REL-2 NE".
           IF P = ADDRESS OF Y DISPLAY "REL-3 EQ" ELSE DISPLAY "REL-3 NE".
           IF P NOT = ADDRESS OF Y
               DISPLAY "REL-4 NE" ELSE DISPLAY "REL-4 EQ".
           IF ADDRESS OF X <> ADDRESS OF Y
               DISPLAY "REL-5 NE" ELSE DISPLAY "REL-5 EQ".
           IF P = ADDRESS OF Y OR ADDRESS OF X
               DISPLAY "REL-6 ABBREVIATED" ELSE DISPLAY "REL-6 NONE".
           SET PP TO ENTRY "PB1021AI".
           IF PP = ADDRESS OF PROGRAM "PB1021AI"
               DISPLAY "REL-7 EQ" ELSE DISPLAY "REL-7 NE".
           IF ADDRESS OF PROGRAM "PB1021AI" = NULL
               DISPLAY "REL-8 NULL" ELSE DISPLAY "REL-8 NOT-NULL".
           INVOKE PB1021K "NEW" RETURNING O.
           INVOKE O "ECHO" USING ADDRESS OF X RETURNING Q.
           IF Q = ADDRESS OF X DISPLAY "INV-1 OK" ELSE DISPLAY "INV-1 BAD".
           INVOKE O "ECHO" USING BY REFERENCE ADDRESS OF Y RETURNING Q.
           IF Q = ADDRESS OF Y DISPLAY "INV-2 OK" ELSE DISPLAY "INV-2 BAD".
           INVOKE O "ECHO" USING BY CONTENT ADDRESS OF X RETURNING Q.
           IF Q = ADDRESS OF X DISPLAY "INV-3 OK" ELSE DISPLAY "INV-3 BAD".
           IF O :: "ECHO" (ADDRESS OF Y) = ADDRESS OF Y
               DISPLAY "INV-4 OK" ELSE DISPLAY "INV-4 BAD".
           INVOKE O "WHICH" USING ADDRESS OF PROGRAM "PB1021AI"
               RETURNING R.
           DISPLAY "INV-5 " R.
           STOP RUN.
       END PROGRAM PB1021AI.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1021K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ECHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       01 LR USAGE POINTER.
       PROCEDURE DIVISION USING LP RETURNING LR.
           SET LR TO LP.
           SET LP TO NULL.
           GOBACK.
       END METHOD ECHO.
       METHOD-ID. WHICH.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LPP USAGE PROGRAM-POINTER.
       01 LR PIC X(4).
       PROCEDURE DIVISION USING LPP RETURNING LR.
           IF LPP = NULL MOVE "NULL" TO LR ELSE MOVE "PROG" TO LR.
           GOBACK.
       END METHOD WHICH.
       END OBJECT.
       END CLASS PB1021K.
