      *>--- conformance-case
      *> id: pb354_start_generic_key_ok
      *> rules: [GR-12.4.5.12.4-4, SR-14.9.41.3-6, FMT-12.4.5.12.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 §14.9.41.3 SR6 — the COMPLEMENT of the screen
      *> kb/Work PB354 tightened. SR6 admits data-name-1 that is
      *>   a) "A data item specified as a prime or alternate record key
      *>      associated with file-name-1", or
      *>   b) a data item whose "leftmost character position within a
      *>      record of the file corresponds to the leftmost character
      *>      position of a prime or alternate record key" (b 1.), that
      *>      "has the same class, category, and usage as that record
      *>      key" (b 2.) and whose "length is not greater than the
      *>      length of that record key" (b 3.).
      *> A screen is evidence about what it RETURNED, never about what
      *> it dropped, so every accepting shape is walked here:
      *>   K1  the declared prime key itself                  (SR6 a)
      *>   K2  an item at the prime key's identical BYTE
      *>       POSITIONS in a SECOND record description —
      *>       §12.4.5.12.4 GR4 makes those positions
      *>       "implicitly referenced as keys for all other
      *>       record description entries"                    (SR6 a)
      *>   K3  a SHORTER same-class/category/usage item at the
      *>       prime key's leftmost position: a generic key,
      *>       which compares on its own length              (SR6 b)
      *>   K4  the declared alternate key itself              (SR6 a)
      *>   K5  a shorter generic key over the ALTERNATE key   (SR6 b)
      *> The three records are keyed AAAAAA / AABBBB / BBBBBB with
      *> alternate keys AA01 / AA02 / BB01, so a 3-character generic
      *> prime key "AAB" selects the SECOND record (not the first,
      *> which "AAA" would) and a 2-character generic alternate "BB"
      *> selects the third — each answer is unreachable by accident.
      *> §14.9.30.4 GR21 (sequential arm b) makes the START-selected
      *> record the one the following READ NEXT delivers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P354GENK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "p354genk.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY   PIC X(6).
          05 IX-ALT   PIC X(4).
          05 IX-DATA  PIC X(6).
       01 IX-VIEW.
          05 VW-KEY   PIC X(6).
          05 VW-ALT   PIC X(4).
          05 VW-TAIL  PIC X(6).
       01 IX-GEN.
          05 GN-PFX   PIC X(3).
          05 GN-FILL  PIC X(3).
          05 GN-APFX  PIC X(2).
          05 GN-REST  PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AAAAAA" TO IX-KEY
           MOVE "AA01"   TO IX-ALT
           MOVE "REC-01" TO IX-DATA
           WRITE IX-REC
           MOVE "AABBBB" TO IX-KEY
           MOVE "AA02"   TO IX-ALT
           MOVE "REC-02" TO IX-DATA
           WRITE IX-REC
           MOVE "BBBBBB" TO IX-KEY
           MOVE "BB01"   TO IX-ALT
           MOVE "REC-03" TO IX-DATA
           WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
      *> K1 — the declared prime key (SR6 a).
           MOVE "AABBBB" TO IX-KEY
           START IXF KEY IS = IX-KEY
               INVALID KEY DISPLAY "K1-INV"
               NOT INVALID KEY DISPLAY "K1-OK"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "K1=" IX-DATA
      *> K2 — the same byte positions in a second record description
      *> (§12.4.5.12.4 GR4 makes them the prime key implicitly).
           MOVE "BBBBBB" TO VW-KEY
           START IXF KEY IS = VW-KEY
               INVALID KEY DISPLAY "K2-INV"
               NOT INVALID KEY DISPLAY "K2-OK"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "K2=" IX-DATA
      *> K3 — a 3-character generic prime key (SR6 b).
           MOVE "AAB" TO GN-PFX
           START IXF KEY IS = GN-PFX
               INVALID KEY DISPLAY "K3-INV"
               NOT INVALID KEY DISPLAY "K3-OK"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "K3=" IX-DATA
      *> K4 — the declared alternate key (SR6 a).
           MOVE "AA02" TO IX-ALT
           START IXF KEY IS = IX-ALT
               INVALID KEY DISPLAY "K4-INV"
               NOT INVALID KEY DISPLAY "K4-OK"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "K4=" IX-DATA
      *> K5 — a 2-character generic ALTERNATE key (SR6 b).
           MOVE "BB" TO GN-APFX
           START IXF KEY IS = GN-APFX
               INVALID KEY DISPLAY "K5-INV"
               NOT INVALID KEY DISPLAY "K5-OK"
           END-START
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "K5=" IX-DATA
           CLOSE IXF
           STOP RUN.
