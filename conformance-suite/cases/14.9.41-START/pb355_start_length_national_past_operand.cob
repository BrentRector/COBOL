      *>--- conformance-case
      *> id: pb355_start_length_national_past_operand
      *> rules: [GR-14.9.41.4-13, GR-14.9.41.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> !! THE SAME RULE ON A NATIONAL KEY, WHERE THE INVENTED PAD IS
      *> NOT EVEN A CHARACTER (kb/Work PB355). ISO 1989:2023
      *> 14.9.41.4 GR17 a): "The specified key is set up by moving the
      *> relevant parts of the record area into a temporary data area."
      *> GR13 fixes the LENGTH unit: "if data-name-1 or
      *> record-key-name-1 is of class national, arithmetic-expression-1
      *> is the number of national character positions" - so LENGTH 2
      *> is TWO NATIONAL POSITIONS, four bytes of the eight-byte key.
      *> GN-PFX is ONE national position (14.9.41.3 SR6 b): leftmost-
      *> coincident with the prime key, same class, category and usage,
      *> and shorter), so a LENGTH of 2 counts one position PAST it and
      *> the missing position has to come from the RECORD AREA.
      *> Taking data-name-1's own two bytes and padding to four with
      *> the alphanumeric space manufactures 0x20 0x20 where a national
      *> position is a UTF-16BE PAIR - 14.9.30.4 GR15 says a trailing
      *> national space is "the national space character", two bytes -
      *> so the padded reading does not merely search for the wrong
      *> value, it searches for a value no record area can hold.
      *>
      *> THE FILE (ascending prime keys, 14.9.51):
      *>   N"AAAA" ONE  /  N"ABBB" TWO  /  N"ACCC" THREE
      *> First two positions of each key: AA, AB, AC.
      *> 14.9.30.4 GR21 b) makes the START-selected record the one the
      *> following READ NEXT delivers; 9.1.13.2 rule 1 gives '00'.
      *>
      *> DERIVATION:
      *>  N1  MOVE N"AB" to the 4-position key space-fills the area to
      *>      N"AB  ". LENGTH 2 -> the temporary area is the key's first
      *>      TWO positions IN THE AREA, N"AB"; each record's key is cut
      *>      to two positions (AA / AB / AC) and EQUAL stops on ABBB
      *>                                                    -> TWO.
      *>      From GN-PFX alone: 00 41 padded with 20 20, which equals
      *>      none of 00 41 00 41 / 00 41 00 42 / 00 41 00 43: '23'.
      *>  N2  the same area, operator GREATER: the first key whose two
      *>      positions exceed N"AB" is ACCC          -> THREE.
      *>      From GN-PFX: 00 41 20 20 exceeds all three (0x20 > 0x00
      *>      at the third byte), so nothing is greater: '23'.
      *>  N3  the complement - LENGTH 1 is no longer than GN-PFX, both
      *>      readings give N"A", and every key starts with it, so the
      *>      FIRST record answers                     -> ONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB355SLN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb355sln.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PRIME
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-PRIME PIC N(4).
          05 IX-DATA  PIC X(6).
       01 IX-VIEW.
          05 GN-PFX   PIC N(1).
          05 GN-REST  PIC N(3).
          05 GN-TAIL  PIC X(6).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE N"AAAA" TO IX-PRIME
           MOVE "ONE   " TO IX-DATA
           WRITE IX-REC
           MOVE N"ABBB" TO IX-PRIME
           MOVE "TWO   " TO IX-DATA
           WRITE IX-REC
           MOVE N"ACCC" TO IX-PRIME
           MOVE "THREE " TO IX-DATA
           WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
      *> N1 - LENGTH 2 reaches one national position past GN-PFX.
           MOVE N"AB" TO IX-PRIME
           START IXF KEY IS = GN-PFX WITH LENGTH 2
               INVALID KEY DISPLAY "N1=INVALID"
           END-START
           DISPLAY "N1-ST=" ST
           READ IXF NEXT AT END DISPLAY "N1=EOF" END-READ
           DISPLAY "N1=" IX-DATA
      *> N2 - the same reach under GREATER.
           MOVE N"AB" TO IX-PRIME
           START IXF KEY IS > GN-PFX WITH LENGTH 2
               INVALID KEY DISPLAY "N2=INVALID"
           END-START
           DISPLAY "N2-ST=" ST
           READ IXF NEXT AT END DISPLAY "N2=EOF" END-READ
           DISPLAY "N2=" IX-DATA
      *> N3 - the complement: a LENGTH no longer than the operand.
           MOVE N"AB" TO IX-PRIME
           START IXF KEY IS >= GN-PFX WITH LENGTH 1
               INVALID KEY DISPLAY "N3=INVALID"
           END-START
           DISPLAY "N3-ST=" ST
           READ IXF NEXT AT END DISPLAY "N3=EOF" END-READ
           DISPLAY "N3=" IX-DATA
           CLOSE IXF
           STOP RUN.
