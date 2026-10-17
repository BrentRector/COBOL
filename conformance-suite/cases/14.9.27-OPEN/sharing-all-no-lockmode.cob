*>--- conformance-case
*> id: sharing-all-no-lockmode
*> rules: [SR-14.9.27.3-8]
*> source-format: free
*> portable: unverified
*>---
*> THE FILE CONTROL CLAUSE ARM of ISO 14.9.27.3 SR8 - disjunct 1: "if the sharing phrase is
*> omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the
*> file control entry for file-name-1 ... the LOCK MODE clause shall be specified in the file
*> control entry for file-name-1". F declares SHARING WITH ALL OTHER with no LOCK MODE clause,
*> and the OPEN below writes NO sharing phrase, so the disjunct holds and the program is
*> ILLEGAL. (The OPEN phrase arm - disjunct 2 - is negative/pb316-open-sharing-all-no-lockmode.)
*> The diagnostic comes from the OPEN, because SR8 is a syntax rule of the OPEN statement about
*> file-name-1 and that is now its only enforcement site.
*> HISTORY, so the OPEN is never "simplified" away again (kb/Work PB319): this fixture used to
*> have NO PROCEDURE DIVISION statement but STOP RUN, and it passed - because a SECOND copy of
*> SR8 in DataBinder.BindFileControl fired off the SELECT clause alone. That copy rejected legal
*> COBOL (a file whose OPEN supplies its own non-ALL sharing phrase; a file never opened at all)
*> and this green golden is what held the defect open. Deleting the OPEN restores the hole.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSR8.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "f.dat" ORGANIZATION IS SEQUENTIAL
        SHARING WITH ALL OTHER.
DATA DIVISION.
FILE SECTION.
FD F.
01 R PIC X(8).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F
    CLOSE F
    STOP RUN.
