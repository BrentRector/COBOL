      *>--- conformance-case
      *> id: pb356_start_first_last_indexed
      *> rules: [GR-14.9.41.4-18, GR-14.9.41.4-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.41.4 GR18 and GR19 - START FIRST / START LAST on an
      *> INDEXED file, and the KEY OF REFERENCE they leave behind.
      *> GR18 - "If FIRST is specified, the file position indicator is
      *>   set to the value of the primary key of the first existing
      *>   logical record in the physical file and the key of reference
      *>   is set to the primary key.  If no records exist in the file,
      *>   the I-O status value in the file connector referenced by
      *>   file-name-1 is set to '23', the invalid key condition
      *>   exists, and the execution of the START statement is
      *>   unsuccessful."
      *> GR19 - the same for LAST and "the last existing logical
      *>   record in the physical file".
      *> THE PRIMARY KEY IS NAMED TWICE IN EACH RULE: it orders the
      *> search AND it becomes the key of reference, so every
      *> subsequent sequential READ walks the PRIME ordering
      *> (14.9.41.4 GR16 last sentence; 14.9.30.4 GR21 b) "Otherwise,
      *> the key of reference is set to the last key of reference in
      *> the file position indicator").
      *> The file is built so the two orderings are EXACTLY INVERTED
      *> and the alternate additionally carries a duplicate pair:
      *>   prime 01 alt CC / prime 02 alt BB / prime 03 alt BB /
      *>   prime 04 alt AA
      *> so prime order is 01 02 03 04 and alternate order is
      *> AA(04) BB(02) BB(03) CC(01).  Every START below runs with the
      *> ALTERNATE established as the key of reference first (a
      *> successful START KEY IS EQUAL, 14.9.41.4 GR16), so an answer
      *> taken under the inherited key of reference is a DIFFERENT
      *> record in every case.
      *>   L1 START LAST then READ NEXT.  GR19 puts "04" in the file
      *>     position indicator; 14.9.30.4 GR21 d) 1. makes the READ
      *>     deliver the first record whose key of reference is >= it,
      *>     which is 04 itself.  L2 then has no record with a greater
      *>     prime key: at end, '10' (14.9.30.4 GR24).
      *>   F1..F4 START FIRST then four READ NEXTs: 01, 02, 03, 04 -
      *>     the prime ordering, which is what GR18's "the key of
      *>     reference is set to the primary key" buys.  Each carries
      *>     '00', not '02': 14.9.30.4 GR27 raises '02' only when "the
      *>     key of reference is an alternate record key", and the
      *>     duplicate BB pair would raise it on the F2 read if the
      *>     alternate had survived the START.
      *>   EF/EL a file with no records at all - both phrases take the
      *>     INVALID KEY imperative and set '23', GR18/GR19's own
      *>     second sentence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P356IXFL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb356ixfl.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PRIME
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
               FILE STATUS IS ST.
           SELECT EXF ASSIGN TO "pb356ixfe.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS EX-PRIME
               FILE STATUS IS ET.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-PRIME PIC XX.
          05 IX-ALT   PIC XX.
       FD EXF.
       01 EX-REC.
          05 EX-PRIME PIC XX.
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       01 ET PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "01" TO IX-PRIME
           MOVE "CC" TO IX-ALT
           WRITE IX-REC
           MOVE "02" TO IX-PRIME
           MOVE "BB" TO IX-ALT
           WRITE IX-REC
           MOVE "03" TO IX-PRIME
           MOVE "BB" TO IX-ALT
           WRITE IX-REC
           MOVE "04" TO IX-PRIME
           MOVE "AA" TO IX-ALT
           WRITE IX-REC
           CLOSE IXF
           OPEN OUTPUT EXF
           CLOSE EXF
      *> ---- LAST after an ALTERNATE key of reference (GR19) ------
           OPEN INPUT IXF
           DISPLAY "OPEN=" ST
           PERFORM ESTABLISH-ALT
           START IXF LAST
               INVALID KEY DISPLAY "L-INV"
               NOT INVALID KEY DISPLAY "L-OK"
           END-START
           DISPLAY "LS=" ST
           READ IXF NEXT AT END DISPLAY "L1-END" END-READ
           DISPLAY "L1=" IX-PRIME "/" IX-ALT "/" ST
           READ IXF NEXT AT END DISPLAY "L2-END" END-READ
           DISPLAY "L2=" ST
      *> ---- FIRST after an ALTERNATE key of reference (GR18) -----
           PERFORM ESTABLISH-ALT
           START IXF FIRST
               INVALID KEY DISPLAY "F-INV"
               NOT INVALID KEY DISPLAY "F-OK"
           END-START
           DISPLAY "FS=" ST
           READ IXF NEXT AT END DISPLAY "F1-END" END-READ
           DISPLAY "F1=" IX-PRIME "/" IX-ALT "/" ST
           READ IXF NEXT AT END DISPLAY "F2-END" END-READ
           DISPLAY "F2=" IX-PRIME "/" IX-ALT "/" ST
           READ IXF NEXT AT END DISPLAY "F3-END" END-READ
           DISPLAY "F3=" IX-PRIME "/" IX-ALT "/" ST
           READ IXF NEXT AT END DISPLAY "F4-END" END-READ
           DISPLAY "F4=" IX-PRIME "/" IX-ALT "/" ST
           CLOSE IXF
      *> ---- a file with no records at all (GR18/GR19 sentence 2) --
           OPEN INPUT EXF
           DISPLAY "EOPEN=" ET
           START EXF FIRST
               INVALID KEY DISPLAY "EF-INV"
               NOT INVALID KEY DISPLAY "EF-OK"
           END-START
           DISPLAY "EF=" ET
           START EXF LAST
               INVALID KEY DISPLAY "EL-INV"
               NOT INVALID KEY DISPLAY "EL-OK"
           END-START
           DISPLAY "EL=" ET
           CLOSE EXF
           STOP RUN.
       ESTABLISH-ALT.
      *> 14.9.41.4 GR16 - the KEY phrase's key becomes the key of
      *> reference, and it is the ALTERNATE here.  "BB" is the
      *> duplicate pair's value, so the START succeeds on 02.
           MOVE "BB" TO IX-ALT
           START IXF KEY IS EQUAL TO IX-ALT
               INVALID KEY DISPLAY "ALT-INV"
           END-START
           DISPLAY "ALT=" ST.
