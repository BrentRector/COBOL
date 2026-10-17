      *>--- conformance-case
      *> id: l1c01_alt_key_equality_collating
      *> rules: [GR-12.4.5.6.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.6.4 r4 — alternate key equality under the key's
      *> collating sequence
      *> THE RULE: "The equality or inequality is based on the collating
      *> sequence used for the file according to the rules for a
      *> relation condition."
      *>   cite.py --check 12.4.5.6.4 "The equality or inequality is
      *>     based on the collating sequence used for the file according
      *>     to the rules for a relation condition" -> OK §12.4.5.6.4 4)
      *>   cite.py --check 12.4.5.6.4 "If the DUPLICATES phrase is not
      *>     specified, the value of the associated alternate record key
      *>     shall not be equal to the value of the same alternate
      *>     record key in another record in the physical file"
      *>     -> OK §12.4.5.6.4 4)
      *>   cite.py --check 12.4.5.7.4 "Alphabet-name-3 applies to record
      *>     keys identified by data-name-1 or record-key-name-1"
      *>     -> OK §12.4.5.7.4 6)
      *>   cite.py --check 12.3.7.4 "ALSO" -> OK §12.3.7.4 7)
      *>     (item k) 6: ALSO puts literal-1 and literal-3 "to the
      *>     same ordinal position in the collating sequence")
      *>   cite.py --check 14.9.51.4 "When an alternate record key of
      *>     the record to be written does not allow duplicates and the
      *>     value of that alternate record key is equal to the value of
      *>     the corresponding alternate record key of a record in the
      *>     file, the I-O status associated with the write file
      *>     connector is set to '22'" -> OK §14.9.51.4 42)
      *> 2002 dir: the key-level COLLATING SEQUENCE OF clause is 2002.
      *> FOLD puts "A"/"a" at one position and "B"/"b" at one position.
      *> AK1 collates by FOLD, AK2 (no clause) by the native sequence;
      *> neither key allows duplicates.
      *> DERIVATION of every .out line:
      *>   W1 0001 AK1=Ab AK2=Ab                        -> W1 00
      *>   W2 0002 AK1=aB: under FOLD aB = Ab           -> W2 IK, W2 22
      *>   W3 0003 AK1=Ac: "c" is unlisted, after every
      *>      listed char, so Ac <> Ab; AK2=aB <> Ab in
      *>      the native sequence                        -> W3 00
      *>   W4 0004 AK1=Qz; AK2=AB <> Ab, <> aB natively  -> W4 00
      *>   read by prime key: 0001, 0003, 0004
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET FOLD IS "A" ALSO "a" "B" ALSO "b".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1C01D.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AK1
               ALTERNATE RECORD KEY IS AK2
               FILE STATUS IS FS
               COLLATING SEQUENCE OF AK1 IS FOLD.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 PK  PIC X(4).
          05 AK1 PIC X(2).
          05 AK2 PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "0001AbAb" TO REC
           WRITE REC INVALID KEY DISPLAY "W1 IK" END-WRITE
           DISPLAY "W1 " FS
           MOVE "0002aBQ1" TO REC
           WRITE REC INVALID KEY DISPLAY "W2 IK" END-WRITE
           DISPLAY "W2 " FS
           MOVE "0003AcaB" TO REC
           WRITE REC INVALID KEY DISPLAY "W3 IK" END-WRITE
           DISPLAY "W3 " FS
           MOVE "0004QzAB" TO REC
           WRITE REC INVALID KEY DISPLAY "W4 IK" END-WRITE
           DISPLAY "W4 " FS
           CLOSE F
           OPEN INPUT F
           PERFORM UNTIL EOF = "Y"
               READ F NEXT RECORD
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "REC " REC
               END-READ
           END-PERFORM
           CLOSE F
           STOP RUN.
