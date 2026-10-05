      *> RESERVE IS BARRED ONLY FROM A LINE SEQUENTIAL FILE (kb/Work PB781).
      *> ISO/IEC 1989:2023 §12.4.5.2 SR12: "If the LINE SEQUENTIAL phrase of the
      *> ORGANIZATION clause is specified, the RESERVE clause shall not be specified."
      *> The rule names the LINE SEQUENTIAL phrase, so a RECORD SEQUENTIAL file (RS) and a
      *> file with no ORGANIZATION clause (DS, sequential RECORD SEQUENTIAL by
      *> §12.4.5.10.3 GR6) keep their RESERVE clauses, beside a LINE SEQUENTIAL file (LS)
      *> that has none. Each file is written with one record and read back:
      *> [LS-1][RS-2][DS-3].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB781RS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LS ASSIGN TO "pb781ls.txt"
               ORGANIZATION LINE SEQUENTIAL.
           SELECT RS ASSIGN TO "pb781rs.dat"
               RESERVE 2 AREAS
               ORGANIZATION RECORD SEQUENTIAL.
           SELECT DS ASSIGN TO "pb781ds.dat"
               RESERVE 3 AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD LS.
       01 LS-REC PIC X(4).
       FD RS.
       01 RS-REC PIC X(4).
       FD DS.
       01 DS-REC PIC X(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT LS RS DS
           MOVE "LS-1" TO LS-REC
           WRITE LS-REC
           MOVE "RS-2" TO RS-REC
           WRITE RS-REC
           MOVE "DS-3" TO DS-REC
           WRITE DS-REC
           CLOSE LS RS DS
           OPEN INPUT LS RS DS
           READ LS
           READ RS
           READ DS
           DISPLAY "[" LS-REC "][" RS-REC "][" DS-REC "]"
           CLOSE LS RS DS
           STOP RUN.
