      *> A KEY-LEVEL COLLATING SEQUENCE CLAUSE NAMES A QUALIFIED KEY (kb/Work PB1075).
      *> ISO/IEC 1989:2023 §12.4.5.7.2 Format 2: COLLATING SEQUENCE OF {data-name-1 |
      *> record-key-name-1}... IS alphabet-name-3. data-name-1 is a user-defined word, and
      *> §8.4.2.2.1 makes qualification the way it is made unique — IX-KEY is declared in
      *> both files' records here, so `OF IX-KEY IN IX-REC` is the natural spelling. It was a
      *> parse error (COBOL0312 unexpected 'IN').
      *> Expected, by the rules: REV orders "Z" first and "A" last (§12.3.7.4 GR7: the THROUGH
      *> phrase "is assigned a successive ascending position in the collating sequence being
      *> specified", literal-1 first); §12.4.5.7.4 GR6 "Alphabet-name-3 applies to record
      *> keys identified by data-name-1", so IX's keys ascend Z, M, A. START ... NOT LESS THAN
      *> "Z" positions at the first record and READ NEXT then returns ZZZ, MMM, AAA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1075QK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" THRU "A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "pb1075ix.dat"
               ORGANIZATION INDEXED ACCESS MODE DYNAMIC
               RECORD KEY IS IX-KEY IN IX-REC
               COLLATING SEQUENCE OF IX-KEY IN IX-REC IS REV.
           SELECT IY ASSIGN TO "pb1075iy.dat"
               ORGANIZATION INDEXED ACCESS MODE DYNAMIC
               RECORD KEY IS IX-KEY IN IY-REC.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IX-REC.
          05 IX-KEY PIC X.
          05 IX-DAT PIC X(3).
       FD IY.
       01 IY-REC.
          05 IX-KEY PIC X.
          05 IY-DAT PIC X(3).
       PROCEDURE DIVISION.
           OPEN OUTPUT IX
           MOVE "A" TO IX-KEY IN IX-REC
           MOVE "AAA" TO IX-DAT
           WRITE IX-REC
           MOVE "M" TO IX-KEY IN IX-REC
           MOVE "MMM" TO IX-DAT
           WRITE IX-REC
           MOVE "Z" TO IX-KEY IN IX-REC
           MOVE "ZZZ" TO IX-DAT
           WRITE IX-REC
           CLOSE IX
           OPEN INPUT IX
           MOVE "Z" TO IX-KEY IN IX-REC
           START IX KEY NOT LESS THAN IX-KEY IN IX-REC
           PERFORM 3 TIMES
               READ IX NEXT
               DISPLAY IX-DAT
           END-PERFORM
           CLOSE IX
           STOP RUN.
