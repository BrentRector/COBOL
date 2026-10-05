      *> kb/Work PB756 - the ALTERNATE RECORD KEY clause's printed spellings, which the tightening of
      *> `RECORD?` to `RECORD` must keep. ISO 12.4.5.6.2 prints `ALTERNATE RECORD KEY IS data-name-1 ...`
      *> with ALTERNATE and RECORD underlined and KEY and IS plain (printed page 350 / folio 320: RECORD's
      *> box 131.86-171.34 carries a rule at 133.25-170.36, 94.0% cover; KEY's and IS's boxes carry none).
      *>   cite.py --check 8.3.2.4.3 "uppercase words that are not underlined are called optional words"
      *>     -> OK 8.3.2.4.3
      *> So `ALTERNATE RECORD KEY IS AK1`, `ALTERNATE RECORD AK2` and `ALTERNATE RECORD KEY AK3` are all
      *> conforming, at every edition (the clause and its words are unchanged since 1985); `ALTERNATE KEY
      *> IS k` is negative/pb756-alternate-record-omitted.
      *>
      *> EXPECTED VALUES, DERIVED: 12.4.5.6.4 GR1 - "An ALTERNATE RECORD KEY clause specifies an alternate
      *> record key for the file with which this clause is associated." Each of the three clauses declares
      *> a key, so a random READ naming it as the key of reference (the 14.9.30.2 KEY phrase)
      *> retrieves the record whose key equals the value moved into it: record 01 carries AK1=AAAA,
      *> AK2=BBBB, AK3=CCCC and record 02 carries AK1=DDDD, AK2=EEEE, AK3=FFFF, so reading by AK1=DDDD,
      *> AK2=BBBB and AK3=FFFF returns primary keys 02, 01 and 02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB756ALTSP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb756-alt-spellings.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-AK1
               ALTERNATE RECORD R-AK2
               ALTERNATE RECORD KEY R-AK3 WITH DUPLICATES
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-AK1 PIC X(4).
          05 R-AK2 PIC X(4).
          05 R-AK3 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "01AAAABBBBCCCC" TO R.
           WRITE R.
           DISPLAY "W1=" WS-FS.
           MOVE "02DDDDEEEEFFFF" TO R.
           WRITE R.
           DISPLAY "W2=" WS-FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "DDDD" TO R-AK1.
           READ F KEY IS R-AK1
               INVALID KEY DISPLAY "AK1 MISSING"
               NOT INVALID KEY DISPLAY "AK1->" R-KEY
           END-READ.
           MOVE "BBBB" TO R-AK2.
           READ F KEY IS R-AK2
               INVALID KEY DISPLAY "AK2 MISSING"
               NOT INVALID KEY DISPLAY "AK2->" R-KEY
           END-READ.
           MOVE "FFFF" TO R-AK3.
           READ F KEY IS R-AK3
               INVALID KEY DISPLAY "AK3 MISSING"
               NOT INVALID KEY DISPLAY "AK3->" R-KEY
           END-READ.
           CLOSE F.
           DISPLAY "DONE".
           STOP RUN.
