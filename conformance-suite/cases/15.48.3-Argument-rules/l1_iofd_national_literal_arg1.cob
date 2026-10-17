*>--- conformance-case
*> id: l1_iofd_national_literal_arg1
*> rules: [AR-15.48.3-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.3 r1 — "Argument-1 shall be a national or alphanumeric
      *> literal": the NATIONAL alternative, which nothing in the corpus exercised.
      *>
      *> ⛔ THE RULE NAMES TWO CLASSES AND ONE SHAPE, and only the alphanumeric
      *> class was ever written. Every existing INTEGER-OF-FORMATTED-DATE golden
      *> passes an alphanumeric literal, and the two negative fixtures around this
      *> function both constrain ARGUMENT-2 (r3), so the national arm of r1 has
      *> never been contradicted — a rule half-exercised reads as enforced. Both
      *> alternatives are written here, side by side, and must return the SAME
      *> integer: the class of the format literal changes how the argument is
      *> spelled, never what date it denotes.
      *>
      *> §15.48.3 r3 — "Argument-2 shall be a data item of the same type as
      *> argument-1" — forces the pairing: an alphanumeric format takes a PIC X
      *> item, a national format a PIC N item. (The mismatched pairing is already
      *> pinned the other way by negative/pb58-iofd-national.)
      *>
      *> THE VALUE, from §15.48.4 r1 ("the integer date form equivalent of the date
      *> represented by argument-2 when analyzed according to argument-1") over
      *> §15.5.2 ("a number of days succeeding December 31, 1600", starting date
      *> Monday, January 1, 1601): 1995-02-15 is 143,950 days after 1601-01-01, so
      *> its integer date form is 143951. "YYYYMMDD" is the §15.3.1.2 basic
      *> calendar date format in both classes — the format's eight characters are
      *> 'Y','M','D' letters, which the national character repertoire contains.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A8   PIC X(8) VALUE "19950215".
       01 ND8  PIC N(8) VALUE N"19950215".
       01 R7   PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
      *> r1's ALPHANUMERIC alternative, with r3's alphanumeric argument-2.
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" A8)
           DISPLAY "ANUM-LITERAL=" R7
      *> r1's NATIONAL alternative, with r3's national argument-2.
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE(N"YYYYMMDD" ND8)
           DISPLAY "NAT-LITERAL=" R7
           STOP RUN.
       END PROGRAM L1IFD02.
