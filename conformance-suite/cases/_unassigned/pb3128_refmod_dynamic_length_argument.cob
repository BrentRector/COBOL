*>--- conformance-case
*> id: pb3128_refmod_dynamic_length_argument
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3128 - a REFERENCE-MODIFIED slice of a DYNAMIC LENGTH item is a legal argument whose
      *> extent is known only at run time. 8.5.1.10.4: "A dynamic-length elementary item that is used as
      *> a sending operand or is reference-modified is treated as a fixed-length data item whose length
      *> is the dynamic-length elementary item's current length" (OK 8.5.1.10.4), and 8.5.1.10.1 bounds
      *> that length by the item's maximum size, the smallest of the LIMIT and "the maximum permitted by
      *> the implementor" (OK 8.5.1.10.1). 14.8.2.2 rule 1 bounds a group formal by the argument's bytes
      *> (OK 14.8.2.2 1)), measured for a run-time slice by its MAXIMUM extent (docs/CONFORMANCE.md
      *> D-RM1 a)): D has no LIMIT, so every group formal is admitted at compile time and the evaluated
      *> slice decides at the boundary (the LIMIT 3 twin is refused:
      *> negative/pb3128-refmod-dynamic-limit-below-group-formal). Before the fix the slice measured
      *> ZERO positions and the typed INVOKE was refused (COBOLNET0828 "larger than the argument (0)").
      *> DERIVATION (D = "ABCDEFGHIJ", current length 10, before each leg; LG is a 4-character group; TG
      *> displays LG then moves "wxyz" to it; D keeps its length 10 - the stores are into slices):
      *>   typed     D(1:N) N=6   6 >= 4   -> TG:ABCD|  d=wxyzEFGHIJ|10
      *>   typed     D(3:)        8 >= 4   -> TG:CDEF|  d=ABwxyzGHIJ|10
      *>   typed     D(1:N) N=2   2 < 4: a copy, written back D(1:2) -> TG:AB  |  d=wxCDEFGHIJ|10
      *>   universal D(N:)  N=5   6 >= 4   -> TG:EFGH|  d=ABCDwxyzIJ|10
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3128DL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C3128DL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C3128DL.
       01 U  USAGE OBJECT REFERENCE.
       01 D  PIC X DYNAMIC LENGTH.
       01 N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C3128DL "NEW" RETURNING T
           SET U TO T
           MOVE "ABCDEFGHIJ" TO D
           MOVE 6 TO N
           INVOKE T "TG" USING D(1:N)
           DISPLAY "d=" D "|" FUNCTION LENGTH(D)
           MOVE "ABCDEFGHIJ" TO D
           INVOKE T "TG" USING D(3:)
           DISPLAY "d=" D "|" FUNCTION LENGTH(D)
           MOVE "ABCDEFGHIJ" TO D
           MOVE 2 TO N
           INVOKE T "TG" USING D(1:N)
           DISPLAY "d=" D "|" FUNCTION LENGTH(D)
           MOVE "ABCDEFGHIJ" TO D
           MOVE 5 TO N
           INVOKE U "TG" USING D(N:)
           DISPLAY "d=" D "|" FUNCTION LENGTH(D)
           STOP RUN.
       END PROGRAM PB3128DL.

       IDENTIFICATION DIVISION.
       CLASS-ID. C3128DL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C3128DL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG "|"
           MOVE "wxyz" TO LG.
       END METHOD TG.
       END OBJECT.
       END CLASS C3128DL.
