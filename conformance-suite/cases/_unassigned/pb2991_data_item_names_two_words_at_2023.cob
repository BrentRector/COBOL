*>--- conformance-case
*> id: pb2991_data_item_names_two_words_at_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2991 - the 2023 side of pb2991_data_item_names_edition_spelled: Annex E.2 item 14 deleted
      *> (03C2,03C3) at 2023, so FINAL SIGMA and SIGMA are two words (ISO 8.1.3.2 GR4), and a name held in a
      *> data item names exactly the word it is spelled as (the edition spelling is the identity at 2023):
      *>   1. CALL WS-FINAL finds no program (PROGRAM-ID. Zσ is another word); 14.9.4.4 GR3 b: the program
      *>      cannot be located, the ON EXCEPTION imperative runs.
      *>   2. CALL WS-SIGMA finds it (the control: the same item holding the word as spelled).
      *>   3. SET PP TO ENTRY WS-FINAL: "If the runtime system cannot locate the program, the EC-PROGRAM-NOT-
      *>      FOUND exception condition is set to exist and the value of the address-identifier is the
      *>      predefined address NULL" (8.4.3.13.4 GR4), while WS-SIGMA locates the program.
      *>   4. INVOKE U WS-FMETHOD finds no method (Xσ is another word): 14.9.23.4 GR7 b sets EC-OO-METHOD, the
      *>      declarative prints it and RESUME AT NEXT STATEMENT continues after the INVOKE (as the 2002 sample
      *>      l1_invoke_dotnet_member_universal); WS-SMETHOD invokes the method.
      *> Each leg fails if the pre-2023 spelling were applied at 2023.
       >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2991N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2991NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2991NC.
       01 U USAGE OBJECT REFERENCE.
       01 PP USAGE PROGRAM-POINTER.
       01 WS-FINAL PIC X(8) VALUE "Zς".
       01 WS-SIGMA PIC X(8) VALUE "Zσ".
       01 WS-FMETHOD PIC X(8) VALUE "Xς".
       01 WS-SMETHOD PIC X(8) VALUE "Xσ".
       PROCEDURE DIVISION.
       DECLARATIVES.
       HM SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       HM-P.
           DISPLAY "EC-OO-METHOD".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL WS-FINAL
               ON EXCEPTION DISPLAY "NOT FOUND FINAL"
               NOT ON EXCEPTION DISPLAY "FOUND FINAL"
           END-CALL.
           CALL WS-SIGMA
               ON EXCEPTION DISPLAY "NOT FOUND SIGMA"
               NOT ON EXCEPTION DISPLAY "FOUND SIGMA"
           END-CALL.
           SET PP TO ENTRY WS-FINAL.
           IF PP = NULL
               DISPLAY "POINTER FINAL NULL"
           ELSE
               DISPLAY "POINTER FINAL SET"
           END-IF.
           SET PP TO ENTRY WS-SIGMA.
           IF PP = NULL
               DISPLAY "POINTER SIGMA NULL"
           ELSE
               DISPLAY "POINTER SIGMA SET"
           END-IF.
           INVOKE PB2991NC "NEW" RETURNING O.
           SET U TO O.
           INVOKE U WS-FMETHOD.
           DISPLAY "AFTER FINAL".
           INVOKE U WS-SMETHOD.
           DISPLAY "AFTER SIGMA".
           STOP RUN.
       END PROGRAM PB2991N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. Zσ.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK.
       END PROGRAM Zσ.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2991NC INHERITS FROM BASE.
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
       END CLASS PB2991NC.
