      *>--- conformance-case
      *> id: pb602_start_key_identity
      *> rules: [SR-14.9.41.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB602 - "SHALL BE THE DATA ITEM" IS AN IDENTITY OVER DATA ITEMS. This is the POSITIVE control
      *> for it: every form below IS the item its rule names, or is a reference whose identity the standard makes
      *> its own, and all of them must keep working. The refusals are
      *> conformance-suite/cases/14.9.41-START/pb602-start-key-refmod-slice.cob and pb602-start-key-renames-alias.cob.
      *>
      *> ISO 14.9.41.3 SR5 - "For relative files, data-name-1, if specified, shall be the data item specified in
      *>   the RELATIVE KEY clause in the associated file control entry."
      *> ISO 14.9.41.3 SR6 - the indexed operand is a) a prime or alternate record key of the file, or b) a
      *>   generic key within a record of the file.
      *> ISO 13.18.45.4 GR1 - "When the THROUGH phrase is not specified, all of the data attributes of
      *>   data-name-2 become the data attributes of data-name-1 and the storage area occupied by data-name-2
      *>   becomes the storage area occupied by data-name-1." Attributes AND storage are shared; the NAME is not,
      *>   and L4/L5 below are what pins the sharing after the identity was split out of it.
      *> ISO 13.18.45.4 GR2 - the THROUGH form "defines an alphanumeric group item that includes all elementary
      *>   items starting with data-name-2 ... and concluding with data-name-3".
      *>
      *> EDITION: --std 85. START, RELATIVE and INDEXED organizations and the RENAMES clause are all COBOL-85
      *> constructs and Annex E lists no change to 14.9.41 or 13.18.45, so the oldest edition is where a
      *> mis-gated screen would show first.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> L1INV=NO / L1V=R1   SR5 is satisfied by the RELATIVE KEY item itself, so the START succeeds
      *>                     (14.9.41.4 GR17) and the following READ delivers relative record 1.
      *> L2INV=NO / L2V=K1   SR6 a) is satisfied by the prime record key itself.
      *> L3=WXYZ             GR2 - the THROUGH alias is an alphanumeric group item spanning WS-G1 and WS-G2, so
      *>                     a four-character move fills both.
      *> L4=0007             GR1 - the non-THROUGH alias has WS-RK's attributes (PIC 9(4)) and WS-RK's storage,
      *>                     so MOVE 7 TO RK-ALIAS makes WS-RK 0007. Splitting the IDENTITY question out of the
      *>                     place must not disturb either.
      *> L5=0010             GR1 again, as a RECEIVING arithmetic operand: ADD 3 TO RK-ALIAS is arithmetic on
      *>                     WS-RK's own numeric description.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB602KEYID85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb602rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-RK.
           SELECT IXF ASSIGN TO "pb602ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(2).
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-KEYS.
          05 WS-RK  PIC 9(4).
          05 WS-G1  PIC X(2).
          05 WS-G2  PIC X(2).
       66 RK-ALIAS RENAMES WS-RK.
       66 G-ALIAS RENAMES WS-G1 THROUGH WS-G2.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RLF
           MOVE 1 TO WS-RK
           MOVE "R1" TO RL-REC
           WRITE RL-REC INVALID KEY DISPLAY "WR1=BAD" END-WRITE
           CLOSE RLF
           OPEN OUTPUT IXF
           MOVE "K001" TO IX-KEY
           MOVE "K1" TO IX-VAL
           WRITE IX-REC INVALID KEY DISPLAY "WK1=BAD" END-WRITE
           CLOSE IXF

      *> ---- L1: SR5 satisfied by the RELATIVE KEY item itself
           OPEN INPUT RLF
           MOVE 1 TO WS-RK
           START RLF KEY IS = WS-RK
               INVALID KEY DISPLAY "L1INV=YES"
               NOT INVALID KEY DISPLAY "L1INV=NO"
           END-START
           READ RLF NEXT AT END CONTINUE END-READ
           DISPLAY "L1V=" RL-REC
           CLOSE RLF

      *> ---- L2: SR6 a) satisfied by the prime record key itself
           OPEN INPUT IXF
           MOVE "K001" TO IX-KEY
           START IXF KEY IS = IX-KEY
               INVALID KEY DISPLAY "L2INV=YES"
               NOT INVALID KEY DISPLAY "L2INV=NO"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L2V=" IX-VAL
           CLOSE IXF

      *> ---- L3/L4/L5: the two RENAMES forms keep their attributes and their storage
           MOVE "WXYZ" TO G-ALIAS
           DISPLAY "L3=" WS-G1 WS-G2
           MOVE 7 TO RK-ALIAS
           DISPLAY "L4=" WS-RK
           ADD 3 TO RK-ALIAS
           DISPLAY "L5=" WS-RK
           STOP RUN.
