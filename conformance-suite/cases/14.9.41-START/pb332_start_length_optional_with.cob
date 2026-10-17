*>--- conformance-case
*> id: pb332_start_length_optional_with
*> rules: [FMT-14.9.41.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! WITH IS AN OPTIONAL WORD OF THE START LENGTH PHRASE (kb/Work PB332).
      *> ISO 14.9.41.2 prints `[ WITH LENGTH arithmetic-expression-1 ]` with LENGTH underlined and
      *> WITH NOT underlined - measured off the PDF's vector rectangles and confirmed on the 600 dpi
      *> render of printed folio 754. 5.2.3: an uppercase word "not underlined in general formats"
      *> is an OPTIONAL word; 8.3.2.4.3: such a word "may be specified at the user's option with no
      *> effect on the semantics of the format". So START ... LENGTH n and START ... WITH LENGTH n
      *> are the SAME statement, and the first was rejected COBOL0001 until PB332.
      *> DERIVATION - every line below follows from the printed rules, nothing from the compiler.
      *>  . The file holds AA01, AA02, BB01 (14.9.51 WRITE, ascending prime keys).
      *>  . 14.9.41.4 GR17 b): the temporary key area is "the length specified in the LENGTH clause,
      *>    if specified" - so LENGTH 2 makes it "AA" out of the moved key AAZZ; c) truncates each
      *>    record's key to the same 2 characters; e) 1. sets the file position indicator to "the
      *>    first logical record whose key satisfies the comparison". "AA" >= "AA" holds for AA01,
      *>    the first record, so both L1 and L2 read AA01.
      *>  . With no LENGTH phrase the temporary area is "the length of data-name-1" (GR17 b)) = 4,
      *>    and "AA01" >= "AAZZ" and "AA02" >= "AAZZ" are false while "BB01" >= "AAZZ" is true, so
      *>    N1 reads BB01. That contrast is what proves LENGTH 2 was actually honoured and not
      *>    silently ignored.
      *>  . 14.9.41.4 GR4 updates the I-O status; 9.1.13.2 rule 1 gives '00' for a successfully
      *>    executed input-output statement, which covers both the START and the READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332SWL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb332swl.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AA01" TO IX-KEY MOVE "ONE " TO IX-VAL WRITE IX-REC
           MOVE "AA02" TO IX-KEY MOVE "TWO " TO IX-VAL WRITE IX-REC
           MOVE "BB01" TO IX-KEY MOVE "THRE" TO IX-VAL WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
      *> L1 - the LENGTH phrase written WITHOUT the optional word WITH.
           MOVE "AAZZ" TO IX-KEY
           START IXF KEY IS >= IX-KEY LENGTH 2
               INVALID KEY DISPLAY "L1=INVALID"
           END-START
           DISPLAY "L1-START=" ST1
           READ IXF NEXT RECORD
               AT END DISPLAY "L1=EOF"
           END-READ
           DISPLAY "L1=" IX-KEY "|" IX-VAL "|" ST1
      *> L2 - the identical statement WITH the optional word written.
           MOVE "AAZZ" TO IX-KEY
           START IXF KEY IS >= IX-KEY WITH LENGTH 2
               INVALID KEY DISPLAY "L2=INVALID"
           END-START
           DISPLAY "L2-START=" ST1
           READ IXF NEXT RECORD
               AT END DISPLAY "L2=EOF"
           END-READ
           DISPLAY "L2=" IX-KEY "|" IX-VAL "|" ST1
      *> N1 - no LENGTH phrase at all: the full 4-character key is compared.
           MOVE "AAZZ" TO IX-KEY
           START IXF KEY IS >= IX-KEY
               INVALID KEY DISPLAY "N1=INVALID"
           END-START
           READ IXF NEXT RECORD
               AT END DISPLAY "N1=EOF"
           END-READ
           DISPLAY "N1=" IX-KEY "|" IX-VAL "|" ST1
           CLOSE IXF
           DISPLAY "DONE"
           STOP RUN.
