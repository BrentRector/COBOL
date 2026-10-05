      *> kb/Work PB513 - the SR14-PERMITTED co-clauses of a TYPE entry, and the description that governs.
      *>
      *> ISO 1989:2023 13.16.3 SR14 lists the clauses that MAY share a data description entry with TYPE:
      *> "BASED, CLASS, CONSTANT RECORD, DEFAULT, DESTINATION, entry-name, EXTERNAL, GLOBAL, INVALID,
      *> level-number, OCCURS, PRESENT WHEN, PROPERTY, TYPEDEF, VALIDATE-STATUS, VALUE, and VARYING."
      *> Every entry below writes one of them, so every entry below is LEGAL and the whole program compiles.
      *> The complement - PICTURE, USAGE, JUSTIFIED, SYNCHRONIZED, SIGN, BLANK WHEN ZERO, REDEFINES and
      *> GROUP-USAGE - is conformance:negative/pb513-type-entry-composition (COBOLNET2150).
      *>
      *> WHAT THE SUBJECT'S DESCRIPTION IS, computed from the rules and not read off a run:
      *>   13.18.57.4 GR1 - "the data description entry ... is treated as though it were described with the
      *>     data description clauses of the type declaration", so A, B, C and F are each PIC X(3) - the
      *>     description T declares - and FUNCTION LENGTH of each is 3 (13.18.40.4 GR: each 'X' represents one
      *>     character position, X(3) is three). This is the fact SR14 protects: with the rule unenforced an
      *>     own PICTURE on the subject won instead, and the type's description was silently discarded.
      *>   13.18.57.4 GR3 - the subject's OWN VALUE clause takes precedence over the type declaration's, so
      *>     A shows "abc" and not T's own value (T declares none here; the rule is what makes VALUE legal
      *>     on the subject at all).
      *>   13.18.38 OCCURS (below level 01, 13.18.38.3 SR1 a) - B is 3 occurrences of that same 3-character description; MOVE "xy" TO B(2)
      *>     space-fills to the right (14.9.25.4 GR5 / 14.6.8, an alphanumeric receiver), so B(2) is "xy ".
      *>   TYPEDEF with TYPE - TT is a type declaration whose own description is TYPE T, so F, which is
      *>     TYPE TT, is PIC X(3) through the two GR1 hops.
      *>   A group type - TG declares two elementary items of 2 and 2 character positions, so D is 4
      *>     (8.5.1.6.1 - the size of a group item is the sum of its subordinate items' character positions).
      *>
      *> The TYPE clause and the TYPEDEF clause are COBOL-2002 introductions (Annex E.2), which is why this
      *> golden's introducing edition is 2002; BASED (E is declared, never referenced) and GLOBAL are written
      *> here only to pin that SR14 admits them beside TYPE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB513TC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T IS TYPEDEF PIC X(3).
       01  TT IS TYPEDEF TYPE T.
       01  TG IS TYPEDEF.
           05  TGA PIC X(2).
           05  TGB PIC 9(2).
       01  A TYPE T VALUE "abc".
       01  BG.
           05  B TYPE T OCCURS 3.
       01  C TYPE T IS GLOBAL.
       01  D TYPE TG.
       01  E TYPE T BASED.
       01  F TYPE TT.
       PROCEDURE DIVISION.
           DISPLAY "A=[" A "] LEN=" FUNCTION LENGTH(A)
           MOVE "xy" TO B(2)
           DISPLAY "B2=[" B(2) "] LEN=" FUNCTION LENGTH(B(2))
           MOVE "pqr" TO C
           DISPLAY "C=[" C "] LEN=" FUNCTION LENGTH(C)
           MOVE "ab12" TO D
           DISPLAY "D=[" D "] LEN=" FUNCTION LENGTH(D)
           MOVE "uvw" TO F
           DISPLAY "F=[" F "] LEN=" FUNCTION LENGTH(F)
           STOP RUN.
