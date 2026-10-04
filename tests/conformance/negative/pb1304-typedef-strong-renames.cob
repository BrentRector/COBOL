*> reject-at: 2002 2014 2023
*> kb/Work PB1304 - ISO 13.18.57.3 SR3: "If type-name-1 is described with the STRONG phrase, the subject of the entry
*>   shall not be renamed in whole or in part." The RENAMES clauses of a type are part of the type
*>   (13.18.58.4 GR1), so AB of a STRONG T renames, in part, every group defined with T, and the group built from
*>   T is refused (COBOLNET1532) exactly as a RENAMES written after a strongly-typed record is. The
*>   same would be silently accepted if the cloned aliases skipped the strong-type screen.
*>   cite.py: OK  13.18.57.3 3)  (Syntax rules)
*>   cite.py: OK  13.18.58.4 1)  (General rules)
*> The positive twin is conformance/2002/pb1304_typedef_renames.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1304N2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  T TYPEDEF STRONG.
    05  A PIC X.
    05  B PIC X.
    66  AB RENAMES A THRU B.
01  R TYPE T.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
