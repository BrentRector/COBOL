      *>--- conformance-case
      *> id: pb2502_renames_record_key
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2502 - a level-66 RENAMES item named in RECORD KEY / ALTERNATE RECORD KEY is a legal key,
      *> and the compiler used to accept it and then abort the run unit at the first verb on the file
      *> (NotImplementedCobolFeatureException: RECORD KEY missing or not locatable in the record image).
      *>   cite.py --check 12.4.5.12.3 "category alphanumeric or category national within a record description entry"
      *>     -> OK 12.4.5.12.3 2)
      *>   cite.py --check 13.18.45.3 "shall immediately follow the last data description entry of the associated record description entry"
      *>     -> OK 13.18.45.3 2)   (the RENAMES entries belong to the record description entry)
      *>   cite.py --check 13.18.45.4 "data-name-1 defines an alphanumeric group item"
      *>     -> OK 13.18.45.4 2)   (the THROUGH alias is category alphanumeric)
      *>   cite.py --check 13.18.45.4 "the storage area occupied by data-name-2 becomes the storage area occupied by data-name-1"
      *>     -> OK 13.18.45.4 1)   (the alias without THROUGH is data-name-2's own area and attributes)
      *> So KR (K1 THRU K2, bytes 3-7), AR (G, a group, bytes 8-11) and KD (D, bytes 12-15) are record keys whose
      *> byte windows are the storage their data-name-2 begins (12.4.5.12.4 GR4: the identical byte positions).
      *>
      *> EXPECTED VALUES, DERIVED: record 1 is "aaBBcccXXYYdddd" (KR=BBccc AR=XXYY KD=dddd), record 2 is
      *> "zzAAbbbWWVVeeee" (KR=AAbbb AR=WWVV KD=eeee); record 3 repeats record 1's prime key BBccc, which
      *> 12.4.5.12.4 GR1 forbids ("shall not be equal to the value of the same record key in another record"),
      *> so its WRITE is the duplicate-key failure, I-O status 22 (9.1.13.5 2) b), checked: "to write a record
      *> that would create a duplicate prime record key in a physical indexed file"). A READ naming a key
      *> retrieves the record whose key equals it: KR=AAbbb -> P=zz, AR=XXYY -> P=aa, KD=eeee -> P=zz. START
      *> KR = AAbbb then READ NEXT delivers the records in ascending key order (9.1.8.2, checked: "For indexed
      *> organization the order of sequential access when NEXT is specified or implied on a READ statement is
      *> ascending based on the value of the key of reference"; AAbbb < BBccc): zz, then aa, then the at end
      *> condition, I-O status 10 (9.1.13.1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2502RENKEY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2502-renames-key.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KR
               ALTERNATE RECORD KEY IS AR WITH DUPLICATES
               ALTERNATE RECORD KEY IS KD WITH DUPLICATES
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 P PIC X(2).
          05 K1 PIC X(2).
          05 K2 PIC X(3).
          05 G.
             10 G1 PIC X(2).
             10 G2 PIC X(2).
          05 D PIC X(4).
       66 KR RENAMES K1 THRU K2.
       66 AR RENAMES G.
       66 KD RENAMES D.
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "aaBBcccXXYYdddd" TO R.
           WRITE R.
           DISPLAY "W1=" WS-FS.
           MOVE "zzAAbbbWWVVeeee" TO R.
           WRITE R.
           DISPLAY "W2=" WS-FS.
           MOVE "yyBBcccQQRRffff" TO R.
           WRITE R.
           DISPLAY "W3=" WS-FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "AAbbb" TO KR.
           READ F.
           DISPLAY "KR AAbbb->" P.
           MOVE "XXYY" TO AR.
           READ F KEY IS AR.
           DISPLAY "AR XXYY->" P.
           MOVE "eeee" TO KD.
           READ F KEY IS KD.
           DISPLAY "KD eeee->" P.
           MOVE "AAbbb" TO KR.
           START F KEY IS EQUAL TO KR.
           READ F NEXT.
           DISPLAY "NEXT " P.
           READ F NEXT.
           DISPLAY "NEXT " P.
           READ F NEXT.
           DISPLAY "END " WS-FS.
           CLOSE F.
           STOP RUN.
