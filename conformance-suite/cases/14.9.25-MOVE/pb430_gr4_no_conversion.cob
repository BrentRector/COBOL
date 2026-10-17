      *>--- conformance-case
      *> id: pb430_gr4_no_conversion
      *> rules: [GR-14.9.25.4-4, FMT-13.18.45.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 14.9.25.4 GR4 - the elementary-vs-group decision and its no-conversion clause
      *> (kb/Work PB430).  GR4: "Any move in which the sending operand is either a literal or an elementary
      *> item and the receiving item is an elementary item is an elementary move." and "Any move that is not
      *> an elementary move, and does not reference a variable-length group, is treated exactly as if it were
      *> an alphanumeric to alphanumeric elementary move, except that there is no conversion of data from one
      *> form of internal representation to another."  Both sentences are measured, on BOTH operand sides.
      *> COBOL-85 is the introducing edition: every rule used here is in the 1985 standard, and the
      *> divergences this pins were silent wrong answers at all four editions.
      *>
      *> (1) A LEVEL-66 RENAMES ... THROUGH ALIAS IS A GROUP ITEM.  13.18.45.4 GR2: "When the THROUGH phrase
      *>     is specified, data-name-1 defines an alphanumeric group item that includes all elementary items
      *>     starting with data-name-2 ..."  So a MOVE from it is NOT an elementary move, and the receiver's
      *>     EDITING - which GR6 confines to valid elementary moves - does not apply: the two characters are
      *>     moved as alphanumeric to alphanumeric and space-filled on the right in the 3-position receiver.
      *>     THRU-SEND and TWIN-SEND are the same KIND of thing under GR2 and must agree.  Both [45 ].
      *> (2) THE CONTROL, 13.18.45.4 GR1: "When the THROUGH phrase is not specified, all of the data
      *>     attributes of data-name-2 become the data attributes of data-name-1" - so the alias of an
      *>     ELEMENTARY item is elementary, the move IS an elementary move, and the receiver edits: the
      *>     alphanumeric sender is treated as an unsigned integer 45 and PIC ZZ9 renders it [ 45].
      *> (3) THE SAME DECISION ON THE RECEIVING SIDE.  GR4's first sentence needs BOTH operands elementary,
      *>     so a THROUGH alias RECEIVER makes the move non-elementary too.  GR6 a) drops the operational
      *>     sign only "when an alphanumeric ... data item is a receiving operand" in a valid ELEMENTARY
      *>     move, so the group receiver keeps the DISPLAY overpunch: -123 in PIC S9(3) is 12L in this
      *>     implementation's zoned representation (docs/CONFORMANCE.md A.1 sign convention), and the alias
      *>     receiver must store exactly what the structurally-identical group receiver stores.  Both [12L  ].
      *> (4) "THERE IS NO CONVERSION OF DATA FROM ONE FORM OF INTERNAL REPRESENTATION TO ANOTHER."  A
      *>     PACKED-DECIMAL elementary item sent INTO a group must deposit the same representation bytes it
      *>     contributes when it is sent AS PART OF a group - 13.18.60.4 leaves that representation to the
      *>     implementor (docs/CONFORMANCE.md items 205-215: BCD, trailing sign nibble, 3 bytes for PIC
      *>     9(4) COMP-3), and whatever it is, the two directions of the same clause must agree.  Rendering
      *>     it as zoned DISPLAY digits is a conversion, and it also makes the item occupy 4 character
      *>     positions where its representation occupies 3.  NOCONV=AGREES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB430-GR4-NO-CONVERSION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SPAN.
          05 WS-A  PIC X VALUE "4".
          05 WS-B  PIC X VALUE "5".
          05 WS-C  PIC X VALUE "Z".
       66 WS-THRU RENAMES WS-A THRU WS-B.
       01 WS-TWIN.
          05 WS-T1 PIC X VALUE "4".
          05 WS-T2 PIC X VALUE "5".
       01 WS-FLAT.
          05 WS-D  PIC X(2) VALUE "45".
       66 WS-NOTHRU RENAMES WS-D.
       01 WS-ED PIC ZZ9.
       01 WS-RCV.
          05 WS-RA PIC X(3) VALUE "---".
          05 WS-RB PIC X(2) VALUE "--".
       66 WS-RTHRU RENAMES WS-RA THRU WS-RB.
       01 WS-GRCV.
          05 WS-GA PIC X(3) VALUE "---".
          05 WS-GB PIC X(2) VALUE "--".
       01 WS-SGN PIC S9(3) VALUE -123.
       01 WS-PKG.
          05 WS-P  PIC 9(4) COMP-3 VALUE 1234.
       01 WS-IMG1 PIC X(6).
       01 WS-IMG2.
          05 WS-IMG2A PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE WS-THRU TO WS-ED.
           DISPLAY "THRU-SEND=[" WS-ED "]".
           MOVE WS-TWIN TO WS-ED.
           DISPLAY "TWIN-SEND=[" WS-ED "]".
           MOVE WS-NOTHRU TO WS-ED.
           DISPLAY "NOTHRU-SEND=[" WS-ED "]".
           MOVE WS-SGN TO WS-RTHRU.
           DISPLAY "THRU-RECV=[" WS-RA WS-RB "]".
           MOVE WS-SGN TO WS-GRCV.
           DISPLAY "GRP-RECV=[" WS-GA WS-GB "]".
           MOVE WS-PKG TO WS-IMG1.
           MOVE WS-P TO WS-IMG2.
           IF WS-IMG1 = WS-IMG2A
               DISPLAY "NOCONV=AGREES"
           ELSE
               DISPLAY "NOCONV=DIFFERS"
           END-IF.
           STOP RUN.
