      *>--- conformance-case
      *> id: pb803_sign_default_representation_85
      *> rules: [GR-13.18.52.4-4, GR-13.18.52.4-5, DOC-A.1-177, DOC-A.1-178]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.52.4 GR4 / GR5 b) AT COBOL-85 — the operational-sign representation and the
      *> valid sign set (Annex A.1 items 177 and 178; kb/Work PB803).
      *>
      *> ⛔ WHY AN 85 TWIN EXISTS.  The SIGN clause and the implementor's latitude over the fused
      *> sign are IDENTICAL in every edition COBOL.NET compiles — X3.23-1985 already carried both
      *> GR4's "the implementor shall specify the position and representation" and GR5 b)'s "the
      *> implementor defines what constitutes valid signs" — so the determination is edition-
      *> independent and the CLAIM to be edition-independent is what this file makes falsifiable.
      *> It brackets the supported range against conformance:2023/pb803_sign_default_representation
      *> and conformance:2023/pb803_sign_valid_set, which pin the same answers at the newest edition.
      *> No intrinsic function appears here: FUNCTION BYTE-LENGTH is a COBOL-2002 introduction, so
      *> the width claim rides entirely on the `|` sentinel, which needs no intrinsic to be
      *> refutable (a 4-character signed item would push it out of the window).
      *>
      *> THE DETERMINATION PINNED HERE (docs/CONFORMANCE.md DOC-A.1-177 / -178, default
      *> --sign-encoding=ibm): the sign fuses onto a DIGIT position — trailing when no SIGN clause
      *> applies — and 0-9 become "{ABCDEFGHI" positive / "}JKLMNOPQR" negative.  So -123 is `12L`,
      *> +123 is `12C`, SIGN IS LEADING -123 is `J23`, and the item is 3 character positions.
      *> The class legs read raw characters back through a REDEFINES window: `12C` and `12L` are
      *> valid signs, `12Z` is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB803D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN.
          05 GN-V   PIC S9(3) VALUE -123.
          05 GN-S   PIC X     VALUE "|".
       01 GP.
          05 GP-V   PIC S9(3) VALUE +123.
          05 GP-S   PIC X     VALUE "|".
       01 GL.
          05 GL-V   PIC S9(3) SIGN IS LEADING VALUE -123.
          05 GL-S   PIC X     VALUE "|".
       01 RAW.
          05 R3    PIC X(3).
          05 RT    REDEFINES R3 PIC S9(3) SIGN IS TRAILING.
       01 W4   PIC X(4).
       01 SHOW PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE GN TO W4
           DISPLAY "NEG=[" W4 "]"
           MOVE GP TO W4
           DISPLAY "POS=[" W4 "]"
           MOVE GL TO W4
           DISPLAY "LEAD=[" W4 "]"
           MOVE "12C" TO R3
           PERFORM SHOW-T
           MOVE "12L" TO R3
           PERFORM SHOW-T
           MOVE "12Z" TO R3
           PERFORM SHOW-T
           STOP RUN.
       SHOW-T.
           IF RT IS NUMERIC
              MOVE RT TO SHOW
              DISPLAY "T[" R3 "]=Y " SHOW
           ELSE
              DISPLAY "T[" R3 "]=N"
           END-IF.
