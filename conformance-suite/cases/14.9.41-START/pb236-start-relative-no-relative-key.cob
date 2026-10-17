*>--- conformance-case
*> id: pb236-start-relative-no-relative-key
*> rules: [GR-14.9.41.4-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - ISO 14.9.41.4 GR8: "If the KEY phrase is omitted, the START statement behaves as though
*> KEY IS EQUAL TO data-name-1 had been specified, where data-name-1 is the name of the key specified in the
*> RELATIVE KEY clause associated with file-name-1."
*> RNK is a relative file in ACCESS SEQUENTIAL with NO RELATIVE KEY clause, which 12.4.5.13 permits - it
*> imposes no requirement to write the clause, and the compiler requires it only for random/dynamic access.
*> So the SOURCE is legal and GR8's substitution has NO OPERAND: there is no data-name-1 to compare, and the
*> standard gives the statement no meaning. ISO 4.2.2's last paragraph leaves flagging a general rule to the
*> implementor ("An implementation may, but is not required to, flag violations of such rules"); the
*> alternative to flagging is emitting code that cannot work, so COBOL.NET flags it, in the SAME COBOLNET0862
*> channel the method already uses for SR1/SR3/SR5/SR6/SR8. Before PB236 it compiled clean and aborted the
*> run unit with NotImplementedCobolFeatureException.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236STRTNRK.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RNK ASSIGN TO "pb236strtnrk.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD RNK.
01 R-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT RNK.
    START RNK INVALID KEY CONTINUE END-START.
    CLOSE RNK.
    STOP RUN.
