*>--- conformance-case
*> id: pb513-type-entry-composition
*> rules: [SR-13.16.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.16.3 SR14: "The TYPE clause shall not be specified in the same data description
      *> entry with any clauses except BASED, CLASS, CONSTANT RECORD, DEFAULT, DESTINATION, entry-name,
      *> EXTERNAL, GLOBAL, INVALID, level-number, OCCURS, PRESENT WHEN, PROPERTY, TYPEDEF, VALIDATE-STATUS,
      *> VALUE, and VARYING." PICTURE, USAGE, JUSTIFIED, SYNCHRONIZED, SIGN, BLANK WHEN ZERO, REDEFINES and
      *> GROUP-USAGE are every clause of the 13.16.2 format-1 clause list that is OUTSIDE that set and can be
      *> written on an entry that also carries TYPE, so each entry below is one violation: COBOLNET2150.
      *>
      *> THE HARM IS A WRONG ANSWER, NOT MERE ACCEPTANCE (kb/Work PB513). SR14 is the warrant for the one
      *> description copy, whose receiver-wins rule is safe only while the subject can own none of the clauses
      *> it carries; unenforced, A1's own PICTURE silently DISCARDED the type's declared description and
      *> 01 T IS TYPEDEF PIC X(3). 01 A TYPE T PIC 9(5). gave A a 5-digit numeric item that 13.18.57.4 GR1
      *> never described - defeating 8.5.3's whole purpose. Its SR12 twin for SAME AS is COBOLNET1555.
      *> The permitted co-clauses are the positive golden conformance:2002/pb513_type_entry_composition.
      *> reject-at omits 85 because the TYPE clause is itself a COBOL-2002 introduction there (COBOLNET0900,
      *> a different rule and code).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB513XC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T IS TYPEDEF PIC S9(5).
       01  TG IS TYPEDEF.
           05  TGA PIC N(2) USAGE NATIONAL.
       01  Z PIC X(3).
       01  A1 TYPE T PIC 9(5).
       01  A2 TYPE T USAGE PACKED-DECIMAL.
       01  A3 TYPE T JUSTIFIED RIGHT.
       01  A4 TYPE T SYNCHRONIZED.
       01  A5 TYPE T SIGN IS LEADING SEPARATE.
       01  A6 TYPE T BLANK WHEN ZERO.
       01  A7 REDEFINES Z TYPE T.
       01  A8 TYPE TG GROUP-USAGE NATIONAL.
       PROCEDURE DIVISION.
           STOP RUN.
