*>--- conformance-case
*> id: pb2840_literal_names_edition_spelled
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2840 - before COBOL 2023, Annex C maps FINAL SIGMA U+03C2
      *> to SIGMA U+03C3 (the mapping Annex E.2 item 14 deleted at 2023), so
      *> Xς and Xσ are ONE word (ISO 8.1.3.2 GR3 b): "Equivalence of
      *> uppercase and lowercase basic letters is achieved by folding ...
      *> in accordance with the case mapping described in Annex C", and
      *> GR4 b) for the extended letters. A literal that NAMES a word names
      *> it in the same edition spelling the word was lexed in:
      *>   1. INVOKE O "Xς" names the method METHOD-ID. Xσ
      *>      (14.9.23.3 SR4 b: literal-1 "shall be the name of a method
      *>      contained in the instance interface"); it was COBOLNET0825.
      *>   2. CALL "Zς" reaches PROGRAM-ID. Zσ (14.9.4.4 GR3 b:
      *>      the program named by the literal is located; it was
      *>      EC-PROGRAM-NOT-FOUND).
      *>   3. CALL "Wσ" reaches PROGRAM-ID. PB2840W AS "Wς": the
      *>      AS literal is the program's externalized name (8.3.2.2 2),
      *>      formed in the same spelling as the CALL literal.
      *> Each leg fails if its literal is compared by the 2023 table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2840M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2840CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2840CL.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB2840CL "NEW" RETURNING O.
           INVOKE O "Xς".
           INVOKE O "Xσ".
           CALL "Zς".
           CALL "Wσ".
           STOP RUN.
       END PROGRAM PB2840M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. Zσ.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "CALL SIGMA".
           GOBACK.
       END PROGRAM Zσ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2840W AS "Wς".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "AS LITERAL".
           GOBACK.
       END PROGRAM PB2840W.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2840CL INHERITS FROM BASE.
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
       END CLASS PB2840CL.
