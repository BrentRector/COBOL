      *> reject-at: 2002 2014 2023
      *> kb/Work PB2517, the nested arm.  ISO 13.18.63.3 SR1: "The subject of the entry shall not be a
      *> strongly-typed group item or a variable-length group."  WEAKT is a WEAK type declaration whose group G
      *> carries a VALUE: referenced from an ordinary record it is legal.  STRONGREC is a STRONG declaration
      *> containing a WEAKT member H, and R is described with a TYPE clause that references STRONGREC.  H is
      *> "subordinate to a group item described with the TYPE clause that references a type declaration specifying
      *> the STRONG phrase" (8.5.3.1, second case), so R.H.G -- two TYPE clauses deep -- is a strongly-typed
      *> group item that carries the VALUE (13.18.58.4 GR3).  The strength is composed by R's TYPE clause, not by
      *> the nearest one (H's, which references a weak declaration), so the verdict is anchored at R.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2517N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WEAKT IS TYPEDEF.
          05 G VALUE "AB".
             10 A PIC X.
             10 B PIC X.
       01 STRONGREC IS TYPEDEF STRONG.
          05 H TYPE WEAKT.
       01 R TYPE STRONGREC.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY A OF R
           STOP RUN.
