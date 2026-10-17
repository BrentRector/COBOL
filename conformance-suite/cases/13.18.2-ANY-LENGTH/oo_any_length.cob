*>--- conformance-case
*> id: oo_any_length
*> rules: [SR-13.18.2.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §13.18.2 ANY LENGTH — a METHOD's LINKAGE formal whose length tracks each
      *> activation's argument (GR1): INVOKEd BY REFERENCE with an X(3) and an X(8) argument, the
      *> method's FUNCTION LENGTH and the item's content prove n = 3 then n = 8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALMAINP9AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ALCLSP9AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE ALCLSP9AL.
       01 A3 PIC XXX VALUE "ABC".
       01 A8 PIC X(8) VALUE "ABCDEFGH".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE ALCLSP9AL "NEW" RETURNING OBJ.
           INVOKE OBJ "SHOW" USING A3.
           INVOKE OBJ "SHOW" USING A8.
           STOP RUN.
       END PROGRAM ALMAINP9AL.

       IDENTIFICATION DIVISION.
       CLASS-ID. ALCLSP9AL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N PIC 99.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
       MAIN.
           MOVE FUNCTION LENGTH(L) TO N.
           DISPLAY "LEN=" N " VAL=" L.
       END METHOD SHOW.
       END OBJECT.
       END CLASS ALCLSP9AL.
