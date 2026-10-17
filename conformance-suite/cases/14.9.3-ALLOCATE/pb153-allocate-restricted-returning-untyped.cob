      *>--- conformance-case
      *> id: pb153-allocate-restricted-returning-untyped
      *> rules: [SR-14.9.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.3.3 SR4 - THE CONVERSE OF SR5, AND A SEPARATE ARM:
      *> "If data-name-2 references a restricted data-pointer,
      *> data-name-1 shall be specified and shall reference a typed
      *> data item, and the data item referenced by data-name-2 shall
      *> be restricted to the type of data-name-1." Here the RETURNING
      *> pointer is restricted to TPT but the based item is untyped,
      *> so the restriction can never be satisfied. A fixture for SR5
      *> alone would leave this direction unproven.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 PTR-T TYPEDEF USAGE POINTER TO TPT.
       01 W PIC X(8) BASED.
       01 P TYPE PTR-T.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE W RETURNING P.
           STOP RUN.
