      *> kb/Work PB994 - SORT AND MERGE USE AND GIVE RELATIVE AND INDEXED FILES.
      *>   cite.py --check 14.9.40.3 "If file-name-2 references a relative or an indexed file, its access mode shall
      *>     be sequential or dynamic." -> OK §14.9.40.3 12)  (the keyed files are legal operands)
      *>   cite.py --check 14.9.40.4 "For a relative file, the relative key data item for the first record returned
      *>     has the value 1; for the second record returned, the value 2; etc." -> OK §14.9.40.4 15)
      *>   cite.py --check 14.9.40.3 "If file-name-3 references an indexed file, the first specification of
      *>     data-name-1 shall be associated with an ASCENDING phrase and the data item referenced by that data-name-1
      *>     shall begin at the same byte location within its record and occupy the same number of bytes as the prime
      *>     record key for that file." -> OK §14.9.40.3 9)
      *> The binder used to refuse every relative or indexed USING / GIVING file as "the G5 keyed slice". IXF and RLG are
      *> ACCESS DYNAMIC and RLF, IXH and RLH SEQUENTIAL: both modes SR12 admits for a USING file, in both roles.
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules):
      *>   1  SORT ASCENDING KEY SK USING RLF GIVING IXF. RLF's records, read as if READ NEXT (GR12 b), come in
      *>      ascending relative record number: C3xx, A1yy, B2zz. IXF's prime key IK is bytes 1-2, the same bytes as
      *>      SK, and the first key is ASCENDING (SR9), so the records are written in prime-key order and a sequential
      *>      read of IXF returns A1yy, B2zz, C3xx.
      *>   2  SORT DESCENDING KEY SK USING IXF GIVING RLG. IXF is read in prime-key order (GR12 b, §12.4.5.5.3 GR2 c);
      *>      the sorted order is C3xx, B2zz, A1yy, and RLG (ACCESS DYNAMIC) receives them as relative records 1, 2,
      *>      3 (GR15 b): REL 01 C3xx, REL 02 B2zz, REL 03 A1yy, and after the statement the relative key data item
      *>      "indicates the last record returned": RK2 = 03.
      *>   3  MERGE ASCENDING KEY SK USING IXF IXH GIVING RLH (RLH ACCESS SEQUENTIAL). IXH holds A2aa and D4dd, so
      *>      the merged order is A1yy, A2aa, B2zz, C3xx, D4dd, written as relative records 1..5 (GR12 b), and RK3
      *>      reads 05.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB994KEY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb994key.tmp".
           SELECT RLF ASSIGN TO "pb994rl1.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK.
           SELECT IXF ASSIGN TO "pb994ix1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IK.
           SELECT IXH ASSIGN TO "pb994ix2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IK2.
           SELECT RLG ASSIGN TO "pb994rl2.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK2.
           SELECT RLH ASSIGN TO "pb994rl3.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK3.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC XX.
          05 SV PIC XX.
       FD RLF.
       01 RR PIC X(4).
       FD IXF.
       01 IXR.
          05 IK PIC XX.
          05 IV PIC XX.
       FD IXH.
       01 IXR2.
          05 IK2 PIC XX.
          05 IV2 PIC XX.
       FD RLG.
       01 R2R PIC X(4).
       FD RLH.
       01 R3R PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 99.
       01 RK2 PIC 99.
       01 RK3 PIC 99.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RLF
           MOVE "C3xx" TO RR
           WRITE RR
           MOVE "A1yy" TO RR
           WRITE RR
           MOVE "B2zz" TO RR
           WRITE RR
           CLOSE RLF
           SORT SW ASCENDING KEY SK USING RLF GIVING IXF
           OPEN INPUT IXF
           READ IXF NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           PERFORM UNTIL EOF-FLAG = "Y"
               DISPLAY "1 IX " IXR
               READ IXF NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           END-PERFORM
           CLOSE IXF
           SORT SW DESCENDING KEY SK USING IXF GIVING RLG
           DISPLAY "2 RK2 " RK2
           MOVE "N" TO EOF-FLAG
           OPEN INPUT RLG
           READ RLG NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           PERFORM UNTIL EOF-FLAG = "Y"
               DISPLAY "2 REL " RK2 " " R2R
               READ RLG NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           END-PERFORM
           CLOSE RLG
           OPEN OUTPUT IXH
           MOVE "A2aa" TO IXR2
           WRITE IXR2
           MOVE "D4dd" TO IXR2
           WRITE IXR2
           CLOSE IXH
           MERGE SW ASCENDING KEY SK USING IXF IXH GIVING RLH
           DISPLAY "3 RK3 " RK3
           MOVE "N" TO EOF-FLAG
           OPEN INPUT RLH
           READ RLH NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           PERFORM UNTIL EOF-FLAG = "Y"
               DISPLAY "3 REL " RK3 " " R3R
               READ RLH NEXT RECORD AT END MOVE "Y" TO EOF-FLAG END-READ
           END-PERFORM
           CLOSE RLH
           STOP RUN.
