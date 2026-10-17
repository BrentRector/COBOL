*>--- conformance-case
*> id: pb427_same_type_by_position
*> rules: [SR-14.8.2.2-2, SR-14.9.25.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 8.5.3.1 -- "Two typed items are of the same type when:
      *>   - The items are described with TYPE clauses that reference equivalent type declarations; or
      *>   - The items are described as subordinate items in equivalent type declarations, starting at
      *>     the same relative byte pr bit position and having the same length in bytes or bits."
      *> (the "pr" is the transcription's OCR slip for "or"; quoted verbatim so the cite check is honest)
      *> and "Two type declarations are considered equivalent when they have the same type-name, both have
      *> the same presence or absence of the EXTERNAL clause and the STRONG phrase, and for each elementary
      *> item in one type declaration there is a corresponding elementary item in the other type declaration,
      *> starting at the same relative byte or bit position and having the same length in bytes or bits."
      *>
      *> The four legs below are the FOUR THINGS THAT PREDICATE HAS TO GET RIGHT, and every one of them was
      *> wrong or missing before kb/Work PB427, when the test was a type-NAME string plus a member-NAME path:
      *>   A  alternative 2 across DIFFERENT names -- OUTERG and INNERG are two differently-named subgroups
      *>      of one declaration, both at relative bit position 0 and both 32 bits long, so they ARE of the
      *>      same type and the MOVE is legal.  The name-path test refused it.
      *>   B  alternative 1 -- two whole items described with TYPE clauses referencing one declaration.
      *>   C  the same predicate under 8.8.4.2.3 SR1, the comparison rule, so the MOVE arm and the compare
      *>      arm cannot answer differently.
      *>   D  EQUIVALENT declarations in TWO source elements -- the contained program declares its own
      *>      SHAPE-T with the same name, the same STRONG phrase, no EXTERNAL clause and an identical
      *>      elementary layout, so 8.5.3.1's equivalence holds across the element boundary and both the
      *>      MOVE (14.9.25.3 SR2) and the CALL argument crossing (14.8.2.2, "If either the formal parameter
      *>      or the corresponding argument is a strongly-typed group item, both shall be of the same type")
      *>      are conforming.  The negative pb427-same-type-nonequivalent-declarations is this same shape
      *>      with a NON-equivalent declaration and is rejected.
      *> TYPEDEF ... STRONG is a COBOL-2002 introduction (13.18.58), so 2002 is the introducing edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427SAMETYPE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEST-T IS TYPEDEF STRONG.
          05 OUTERG.
             10 INNERG.
                15 LEAFX PIC X(4).
       01 N1 TYPE NEST-T.
       01 N2 TYPE NEST-T.
       01 SHAPE-T IS TYPEDEF STRONG.
          05 SA PIC X(3).
          05 SB PIC 9(3).
       01 OUTER-X TYPE SHAPE-T IS GLOBAL.
       PROCEDURE DIVISION.
       MAIN.
      *> A -- alternative 2: subordinate items at the same relative position and length.
           MOVE "WXYZ" TO LEAFX OF N1
           MOVE OUTERG OF N1 TO INNERG OF N2
           DISPLAY "A=" LEAFX OF N2
      *> B -- alternative 1: whole typed items, one declaration.
           MOVE SPACES TO LEAFX OF N2
           MOVE N1 TO N2
           DISPLAY "B=" LEAFX OF N2
      *> C -- 8.8.4.2.3 SR1 over the same predicate.
           IF OUTERG OF N1 = INNERG OF N2
               DISPLAY "C=EQUAL"
           ELSE
               DISPLAY "C=UNEQUAL"
           END-IF
      *> D -- equivalent declarations in two source elements.
           MOVE "PQR" TO SA OF OUTER-X
           MOVE 123 TO SB OF OUTER-X
           CALL "PB427SAMETYPEIN" AS NESTED USING OUTER-X
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB427SAMETYPEIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SHAPE-T IS TYPEDEF STRONG.
          05 SA PIC X(3).
          05 SB PIC 9(3).
       01 INNER-Y TYPE SHAPE-T.
       LINKAGE SECTION.
       01 LF TYPE SHAPE-T.
       PROCEDURE DIVISION USING LF.
       CMAIN.
           MOVE OUTER-X TO INNER-Y
           DISPLAY "D1=" SA OF INNER-Y "/" SB OF INNER-Y
           DISPLAY "D2=" SA OF LF "/" SB OF LF
           GOBACK.
       END PROGRAM PB427SAMETYPEIN.
       END PROGRAM PB427SAMETYPE.
