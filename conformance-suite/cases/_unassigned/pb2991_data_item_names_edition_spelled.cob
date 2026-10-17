*>--- conformance-case
*> id: pb2991_data_item_names_edition_spelled
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2991 - the DATA-ITEM twin of pb2840_literal_names_edition_spelled. Before COBOL 2023, Annex C
      *> maps FINAL SIGMA U+03C2 to SIGMA U+03C3 (the mapping Annex E.2 item 14 deleted at 2023), so Xς and Xσ
      *> are ONE word (ISO 8.1.3.2 GR3: "Equivalence of uppercase and lowercase basic letters is achieved by
      *> folding from uppercase to lowercase in accordance with the case mapping described in Annex C"; GR4 for
      *> the extended letters).
      *> A name that is the CONTENT OF A DATA ITEM names the same word the literal of the same characters does:
      *>   1. CALL WS-NAME (14.9.4.4 GR3 b: "The program being called is identified by its program-name or its
      *>      location", the name here being the content of identifier-1).
      *>   2. CANCEL WS-NAME (14.9.5.4 GR1: "The program to be canceled is identified by one of the following").
      *>   3. SET PP TO ENTRY WS-NAME / ADDRESS OF PROGRAM WS-NAME (8.4.3.13.4 GR1 a: "the content of the data
      *>      item referenced by identifier-1").
      *>   4. INVOKE U WS-METHOD, U universal (14.9.23.4 GR2: "Literal-1 or the content of the data item
      *>      referenced by identifier-2" names the method).
      *> Each leg fails (EC-PROGRAM-NOT-FOUND / EC-OO-METHOD) when the item's value is compared by the 2023 table.
      *> DERIVATION of the output: the method prints once; the program Zσ counts its activations in a WORKING-
      *> STORAGE item that CANCEL returns to its initial state (14.9.5.4 GR3: "that program is in its initial
      *> state"): CALL, CALL -> 1, 2; CANCEL; CALL -> 1; CALL through the pointer -> 2; the relation
      *> PP = ADDRESS OF PROGRAM WS-NAME is true because "The operands are equal if they reference the same
      *> address" (8.8.4.2.16) and both reference the one program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2991M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2991CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2991CL.
       01 U USAGE OBJECT REFERENCE.
       01 PP USAGE PROGRAM-POINTER.
       01 WS-NAME PIC X(12).
       01 WS-METHOD PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB2991CL "NEW" RETURNING O.
           SET U TO O.
           MOVE "Xς" TO WS-METHOD.
           INVOKE U WS-METHOD.
           MOVE "Zς" TO WS-NAME.
           CALL WS-NAME.
           CALL WS-NAME.
           CANCEL WS-NAME.
           CALL WS-NAME.
           SET PP TO ENTRY WS-NAME.
           CALL PP.
           IF PP = ADDRESS OF PROGRAM WS-NAME
               DISPLAY "SAME PROGRAM"
           ELSE
               DISPLAY "OTHER PROGRAM"
           END-IF.
           STOP RUN.
       END PROGRAM PB2991M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. Zσ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO CNT.
           DISPLAY "CALL SIGMA " CNT.
           GOBACK.
       END PROGRAM Zσ.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2991CL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Xσ.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD SIGMA".
       END METHOD Xσ.
       END OBJECT.
       END CLASS PB2991CL.
