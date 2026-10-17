      *>--- conformance-case
      *> id: pb356_start_first_last_suppressed
      *> rules: [GR-14.9.41.4-18, GR-14.9.41.4-19]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.41.4 GR18/GR19 meet 12.4.5.6.4 GR6 - START FIRST and
      *> START LAST see EVERY record, including the ones an ALTERNATE
      *> RECORD KEY's SUPPRESS WHEN phrase withholds.
      *> GR18 - "the file position indicator is set to the value of the
      *>   primary key of the first existing logical record in the
      *>   physical file and the key of reference is set to the primary
      *>   key"; GR19 the same for LAST and the last existing record.
      *> 12.4.5.6.4 GR6 - "Alternate record key suppression specifies
      *>   that an ALTERNATE RECORD KEY ACCESS PATH to a particular
      *>   record shall not be provided when the value of data-name-1
      *>   or record-key-name-1 in that record is equal to literal-1."
      *> The suppression is a property of the ALTERNATE path alone, so
      *> a FIRST/LAST that ran under an inherited alternate key of
      *> reference would not merely order the file wrongly - it would
      *> not see the suppressed records AT ALL.
      *> SX has RECORD KEY SX-PRIME and ALTERNATE SX-ALT WITH
      *> DUPLICATES SUPPRESS WHEN "ZZ"; the records are
      *>   prime 01 alt ZZ (suppressed) / prime 02 alt MM /
      *>   prime 03 alt ZZ (suppressed)
      *> so the ALTERNATE path offers exactly one record, 02, while
      *> the physical file holds three.
      *>   A  START KEY IS EQUAL TO SX-ALT on "MM" succeeds and makes
      *>      the ALTERNATE the key of reference (14.9.41.4 GR16).
      *>   F1..F3  START FIRST then three READ NEXTs: GR18 positions on
      *>      primary key "01" - a record with NO alternate path - and
      *>      the reads walk 01, 02, 03 in prime order.  F4 is at end
      *>      ('10', 14.9.30.4 GR24).
      *>   L1  START LAST then READ NEXT: GR19 positions on primary key
      *>      "03", also a suppressed one, and the read delivers it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P356IXSP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SXF ASSIGN TO "pb356ixsp.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS SX-PRIME
               ALTERNATE RECORD KEY IS SX-ALT WITH DUPLICATES
                   SUPPRESS WHEN "ZZ"
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD SXF.
       01 SX-REC.
          05 SX-PRIME PIC XX.
          05 SX-ALT   PIC XX.
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SXF
           MOVE "01" TO SX-PRIME
           MOVE "ZZ" TO SX-ALT
           WRITE SX-REC
           MOVE "02" TO SX-PRIME
           MOVE "MM" TO SX-ALT
           WRITE SX-REC
           MOVE "03" TO SX-PRIME
           MOVE "ZZ" TO SX-ALT
           WRITE SX-REC
           CLOSE SXF
           OPEN INPUT SXF
           DISPLAY "OPEN=" ST
      *> ---- the ALTERNATE becomes the key of reference (GR16) ----
           MOVE "MM" TO SX-ALT
           START SXF KEY IS EQUAL TO SX-ALT
               INVALID KEY DISPLAY "A-INV"
               NOT INVALID KEY DISPLAY "A-OK"
           END-START
           DISPLAY "A=" ST
      *> ---- FIRST sees the suppressed record 01 (GR18) -----------
           START SXF FIRST
               INVALID KEY DISPLAY "F-INV"
               NOT INVALID KEY DISPLAY "F-OK"
           END-START
           DISPLAY "FS=" ST
           READ SXF NEXT AT END DISPLAY "F1-END" END-READ
           DISPLAY "F1=" SX-PRIME "/" SX-ALT "/" ST
           READ SXF NEXT AT END DISPLAY "F2-END" END-READ
           DISPLAY "F2=" SX-PRIME "/" SX-ALT "/" ST
           READ SXF NEXT AT END DISPLAY "F3-END" END-READ
           DISPLAY "F3=" SX-PRIME "/" SX-ALT "/" ST
           READ SXF NEXT AT END DISPLAY "F4-END" END-READ
           DISPLAY "F4=" ST
      *> ---- LAST sees the suppressed record 03 (GR19) ------------
           MOVE "MM" TO SX-ALT
           START SXF KEY IS EQUAL TO SX-ALT
               INVALID KEY DISPLAY "A2-INV"
           END-START
           DISPLAY "A2=" ST
           START SXF LAST
               INVALID KEY DISPLAY "L-INV"
               NOT INVALID KEY DISPLAY "L-OK"
           END-START
           DISPLAY "LS=" ST
           READ SXF NEXT AT END DISPLAY "L1-END" END-READ
           DISPLAY "L1=" SX-PRIME "/" SX-ALT "/" ST
           CLOSE SXF
           STOP RUN.
