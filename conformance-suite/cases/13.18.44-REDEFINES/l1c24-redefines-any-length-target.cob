      *>--- conformance-case
      *> id: l1c24-redefines-any-length-target
      *> rules: [SR-13.18.44.3-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.44.3 SR16 — REDEFINES naming an ANY LENGTH item
      *> "Data-name-2 shall not be described with the ANY LENGTH
      *> clause."
      *>   cite.py --check 13.18.44.3 "Data-name-2 shall not be
      *>   described with the ANY LENGTH clause."
      *>     -> OK §13.18.44.3 16) (Syntax rules)
      *> L1C24B is a contained program; L is a level-1 elementary
      *> linkage item PIC X ANY LENGTH named BY REFERENCE in its
      *> procedure division header (§13.18.2.3 SR1-SR3 satisfied).
      *> M REDEFINES L is otherwise legal: same level 1 (SR2), follows
      *> L directly (SR4, SR10), no OCCURS/VALUE, and SR8 exempts a
      *> level-1 non-EXTERNAL data-name-2. The only violation is SR16.
      *> ANY LENGTH first appears in ISO/IEC 1989:2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24A.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       01 M REDEFINES L PIC X.
       PROCEDURE DIVISION USING BY REFERENCE L.
       B-P.
           DISPLAY M.
           GOBACK.
       END PROGRAM L1C24B.
       END PROGRAM L1C24A.
