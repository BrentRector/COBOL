*>--- conformance-case
*> id: pb323_open_presence_arms
*> rules: [GR-9.1.13.6-5, GR-12.4.5.3-2, GR-14.9.27.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB323PRES.
      *> kb/Work PB323 - the PORTABLE half of the OPEN presence decision.
      *> GR3's authority arm needs a file the process may not read, which
      *> no .cob can arrange, so it is pinned by
      *> unit:FileAuthorityPresenceTests. THIS program pins the other two
      *> answers of the same probe - ABSENT and PRESENT - across all three
      *> organizations and both OPTIONAL states, because PB323 replaced
      *> every connector's File.Exists with the shared three-state
      *> HostFile.Probe and a refactor that got Absent wrong would be a
      *> far louder defect than the one it fixed.
      *> 9.1.13.6 5): I-O status 35 "because an OPEN statement with the
      *> INPUT, I-O, or EXTEND phrase is attempted on a file that is not
      *> described as optional and the physical file is not present".
      *> 14.9.27.4 13): "If the file is not present, and the INPUT phrase
      *> is specified in the OPEN statement, and the OPTIONAL clause is
      *> specified" - Table 18's "Normal open; the first read causes the
      *> at end condition", so '05' then 9.1.13.4 1)'s '10'.
      *> 14.9.27.4 17): "If the file is not present, and the EXTEND or I-O
      *> phrase is specified ... and the OPTIONAL clause is specified, the
      *> OPEN statement creates the file" - '05', and the file is there
      *> afterwards.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb323sq.dat"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL SQO ASSIGN TO "pb323sqo.dat"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT RL ASSIGN TO "pb323rl.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL RLO ASSIGN TO "pb323rlo.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT IX ASSIGN TO "pb323ix.dat"
               ORGANIZATION INDEXED ACCESS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL IXO ASSIGN TO "pb323ixo.dat"
               ORGANIZATION INDEXED ACCESS SEQUENTIAL
               RECORD KEY IS IXO-KEY
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-REC PIC X(8).
       FD SQO.
       01 SQO-REC PIC X(8).
       FD RL.
       01 RL-REC PIC X(8).
       FD RLO.
       01 RLO-REC PIC X(8).
       FD IX.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-PAD PIC X(4).
       FD IXO.
       01 IXO-REC.
          05 IXO-KEY PIC X(4).
          05 IXO-PAD PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- ABSENT, NOT OPTIONAL: 9.1.13.6 5) '35', every organization.
           OPEN INPUT SQ
           DISPLAY "SQ-ABS-IN=" WS-ST
           OPEN I-O RL
           DISPLAY "RL-ABS-IO=" WS-ST
           OPEN EXTEND IX
           DISPLAY "IX-ABS-EX=" WS-ST
      *> ---- ABSENT, OPTIONAL, INPUT: 14.9.27.4 13) - '05', and the
      *> ---- first READ is 9.1.13.4 1)'s at end '10'.
           OPEN INPUT SQO
           DISPLAY "SQO-ABS-IN=" WS-ST
           READ SQO AT END CONTINUE END-READ
           DISPLAY "SQO-READ=" WS-ST
           CLOSE SQO
           OPEN INPUT IXO
           DISPLAY "IXO-ABS-IN=" WS-ST
           READ IXO AT END CONTINUE END-READ
           DISPLAY "IXO-READ=" WS-ST
           CLOSE IXO
      *> ---- ABSENT, OPTIONAL, EXTEND: 14.9.27.4 17) - '05' and CREATED,
      *> ---- so the next OPEN INPUT finds it PRESENT ('00').
           OPEN EXTEND RLO
           DISPLAY "RLO-ABS-EX=" WS-ST
           MOVE "RRRRRRRR" TO RLO-REC
           WRITE RLO-REC
           CLOSE RLO
           OPEN INPUT RLO
           DISPLAY "RLO-PRE-IN=" WS-ST
           READ RLO AT END CONTINUE END-READ
           DISPLAY "RLO-READ=" WS-ST
           DISPLAY "RLO-REC=" RLO-REC
           CLOSE RLO
      *> ---- PRESENT, NOT OPTIONAL: the probe's other answer.
           OPEN OUTPUT SQ
           MOVE "SSSSSSSS" TO SQ-REC
           WRITE SQ-REC
           CLOSE SQ
           OPEN INPUT SQ
           DISPLAY "SQ-PRE-IN=" WS-ST
           READ SQ AT END CONTINUE END-READ
           DISPLAY "SQ-REC=" SQ-REC
           CLOSE SQ
      *> ---- Leave no files behind (the corpus runner reuses its cwd).
           DELETE FILE SQ
           DELETE FILE RLO
           DISPLAY "CLEAN=" WS-ST
           STOP RUN.
