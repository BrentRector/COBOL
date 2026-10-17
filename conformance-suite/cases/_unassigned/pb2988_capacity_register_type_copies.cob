      *>--- conformance-case
      *> id: pb2988_capacity_register_type_copies
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2988 - a CAPACITY IN register written once and
      *> reproduced by every TYPE / SAME AS copy of its entry.
      *> ISO 13.18.38.3 SR30: "Data-name-3 shall not be defined
      *>   elsewhere in the source element. If qualifiers are required
      *>   for uniqueness, it shall be treated as though implicitly
      *>   defined at the same level as the entry containing the OCCURS
      *>   clause" -> OK 13.18.38.3 30). DC is defined ONCE, in ST; A
      *>   and B each reproduce it, and DC OF A / DC OF B tell the two
      *>   copies apart (8.4.2.2.3 SR1). The refusal this replaces
      *>   (COBOLNET1523 at B's expansion) counted the copy as a second
      *>   definition. Each leg fails if the copies share one register.
      *> Expected values, from the standard:
      *>   8.5.1.9.1: no FROM, no VALUE -> "the current capacity is
      *>     initialized to zero".
      *>   8.5.1.9.3: a receiving subscript past the capacity -> "the
      *>     capacity of the table is increased to the value given by
      *>     the subscript": D OF A (1) -> 1, D OF B (3) -> 3.
      *>   14.9.39.4 GR30 (via 8.5.1.9.4): SET DC OF B TO 7 -> B is 7,
      *>     A stays 1; SET DC OF A UP BY 2 -> A is 3.
      *>   SAME AS (P, Q) and TYPE under one group (X, Y; two
      *>   qualifiers) are the same reproduction: 2 4 5 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2988P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF.
          05 K PIC X.
          05 D PIC X OCCURS DYNAMIC CAPACITY IN DC.
       01 A TYPE ST.
       01 B TYPE ST.
       01 P.
          05 E PIC X OCCURS DYNAMIC CAPACITY IN PC.
       01 Q SAME AS P.
       01 SU TYPEDEF.
          05 G.
             10 F PIC X OCCURS DYNAMIC CAPACITY IN FC.
       01 C.
          05 X TYPE SU.
          05 Y TYPE SU.
       PROCEDURE DIVISION.
           DISPLAY "INIT " DC OF A " " DC OF B
           MOVE "X" TO D OF A (1)
           MOVE "Y" TO D OF B (3)
           DISPLAY "GROW " DC OF A " " DC OF B
           SET DC OF B TO 7
           SET DC OF A UP BY 2
           DISPLAY "SET  " DC OF A " " DC OF B
           MOVE "P" TO E OF P (2)
           MOVE "Q" TO E OF Q (4)
           MOVE "X" TO F OF X (5)
           MOVE "Y" TO F OF G OF Y OF C (6)
           DISPLAY "COPY " PC OF P " " PC OF Q " " FC OF X " "
                   FC OF G OF Y
           DISPLAY "ELEM " D OF B (3) E OF Q (4) F OF X (5)
           STOP RUN.
