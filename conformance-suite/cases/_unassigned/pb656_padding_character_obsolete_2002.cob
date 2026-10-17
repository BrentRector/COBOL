      *>--- conformance-case
      *> id: pb656_padding_character_obsolete_2002
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB656 and PB300 - the PADDING CHARACTER clause of the
      *> file control entry is accepted at COBOL-85, OBSOLETE at
      *> COBOL-2002 (the obsolete-element warning COBOLNET0903, never
      *> an error) and removed at 2014 (negative/pb300-padding-clause-
      *> dataname). The repository holds no 2002/2014 text, so the
      *> edges are GnuCOBOL 3.2's (cobol2002.conf: obsolete,
      *> cobol2014.conf: unconformable), CLAUDE.md rule 1. With no
      *> blocking model the clause pads nothing: the record written is
      *> the record read back.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB656PAD.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT PADF ASSIGN TO "pb656pad.dat"
000700         ORGANIZATION IS SEQUENTIAL
000800         PADDING CHARACTER IS "9".
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD PADF.
001200 01 PAD-REC PIC X(3).
001300 PROCEDURE DIVISION.
001400     OPEN OUTPUT PADF.
001500     MOVE "PAD" TO PAD-REC.
001600     WRITE PAD-REC.
001700     CLOSE PADF.
001800     MOVE SPACES TO PAD-REC.
001900     OPEN INPUT PADF.
002000     READ PADF.
002100     DISPLAY "REC=" PAD-REC.
002200     CLOSE PADF.
002300     STOP RUN.
