*> reject-at: 2002 2014 2023
*> kb/Work PB1304 - ISO 13.18.58.4 GR1: the RENAMES clauses of a TYPEDEF are part of the type, so the type's
*>   definition is only as good as its members: 13.18.45.3 SR4 requires data-name-2 and data-name-3 to be names of
*>   items "in the same record", and NOSUCH names nothing in a group defined with T, so the alias on R is
*>   refused at the group that is built from the type (COBOLNET1655, the diagnostic every RENAMES operand
*>   that names nothing draws).
*>   cite.py: OK  13.18.58.4 1)  (General rules)
*>   cite.py: OK  13.18.45.3 4)  (Syntax rules)
*> The positive twin is conformance/2002/pb1304_typedef_renames.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1304N1.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  T TYPEDEF.
    05  A PIC X.
    05  B PIC X.
    66  AB RENAMES A THRU NOSUCH.
01  R TYPE T.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
