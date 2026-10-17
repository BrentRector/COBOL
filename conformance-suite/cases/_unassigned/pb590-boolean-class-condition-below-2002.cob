*>--- conformance-case
*> id: pb590-boolean-class-condition-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The BOOLEAN class condition (ISO 8.8.4.4.2 / 8.8.4.4.4 GR3 e) arrives with boolean data at
      *> COBOL-2002, and the EDITION GATE IS THE RESERVED-WORD TABLE, not a separate introduction check:
      *> BOOLEAN is not a reserved word in COBOL-85, so below 2002 the class condition's user-defined-word
      *> alternative claims it and the word can only name an alphabet-name-1 or a class-name-1. Here it
      *> names neither, so it identifies no resource - ISO 8.4.2.1, "In order to use a resource, a statement
      *> shall contain a reference that uniquely identifies that resource" (COBOLNET1639).
      *>
      *> ⛔ THE COMPLEMENT IS 85/pb571_class_condition_one_table's sibling fact and is deliberately NOT
      *> rejected: `SPECIAL-NAMES. CLASS BOOLEAN IS "0" THROUGH "1".` with `IF X IS BOOLEAN` is conforming
      *> COBOL-85 and compiles, because the grammar puts the keyword alternative AFTER the user-word one.
      *>
      *> ⛔ AND BEFORE kb/Work PB590 THIS PROGRAM COMPILED WITH NO DIAGNOSTIC AT ALL and aborted at run time
      *> with NotImplementedCobolFeatureException "class condition 'BOOLEAN'" - the undefined class-name arm
      *> fell to a loud RUN-TIME stage where 8.4.2.1 wants a bind diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB590NG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(4) VALUE "0101".
       PROCEDURE DIVISION.
       MAIN.
           IF XA IS BOOLEAN
               DISPLAY "BOOL"
           END-IF
           STOP RUN.
