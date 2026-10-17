*>--- conformance-case
*> id: pb2586_unstring_sr2_category_operands
*> rules: [SR-14.9.48.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2586. ISO 14.9.48.3 SR2: "Identifier-1, identifier-2, identifier-3, and
      *> identifier-5 shall reference data items of category alphanumeric or national."
      *> Category alphabetic (PIC A) is a category of its own (8.5.2.1 Table 2), so a PIC A
      *> sender, delimiter or DELIMITER IN receiver is refused (the negative corpus,
      *> pb2586-unstring-alphabetic-*). This golden pins the LEGAL side of the same screen,
      *> which the old screen got wrong in one direction or admitted by accident:
      *> - 8.4.3.3.4 GR6 c): a reference-modified slice of a NUMERIC item is category
      *>   alphanumeric, so NUM(2:3) is a legal sender (T1) and NUM(1:1) a legal
      *>   delimiter (T4); the old screen read the whole item's category and refused both.
      *> - 14.9.48.3 SR4: identifier-4 may be category alphabetic, so a PIC A INTO
      *>   receiver is legal (T2) - the rule SR2 must not be confused with.
      *> - 8.5.2.1: an alphanumeric group is category alphanumeric whatever its leaves
      *>   are, so a group holding a PIC A leaf is a legal sender (T3).
      *> - 15.18.4 r3 / 8.4.3.3.4 GR6: FUNCTION CONCAT is alphabetic only when every argument is
      *>   class alphabetic, and an alphabetic slice stays alphabetic (kb/Work PB73), so a CONCAT
      *>   mixing a PIC A slice with a PIC X slice is alphanumeric and a legal sender (T5); the
      *>   all-alphabetic twin is the negative pb2586-unstring-alphabetic-function-slice-sender.
      *> Expected values are the GR positions: characters are examined left to right
      *> until the delimiter, the examined characters are moved to the receiver (MOVE
      *> alphanumeric-to-alphanumeric pads with spaces) and the delimiter goes to DELIMITER IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586SR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NUM PIC 9(5) VALUE 12345.
       01 SRC2 PIC X(5) VALUE "A1B1C".
       01 SRC3 PIC X(5) VALUE "AB CD".
       01 G.
          05 GA PIC A(2) VALUE "XY".
          05 GB PIC X(3) VALUE "-ZZ".
       01 R1 PIC X(4).
       01 R2 PIC X(4).
       01 R3 PIC X(4).
       01 DL PIC X.
       01 RA PIC A(3).
       PROCEDURE DIVISION.
      *> T1 - slice "234" of the numeric item, delimiter "3": R1 "2", DELIMITER IN "3".
           MOVE SPACES TO R1.
           UNSTRING NUM(2:3) DELIMITED BY "3" INTO R1 DELIMITER IN DL.
           DISPLAY "T1 [" R1 "][" DL "]".
      *> T2 - a category alphabetic INTO receiver (SR4): "AB" padded to three positions.
           UNSTRING SRC3 DELIMITED BY SPACE INTO RA.
           DISPLAY "T2 [" RA "]".
      *> T3 - an alphanumeric group holding a PIC A leaf as the sender.
           MOVE SPACES TO R1 R2.
           UNSTRING G DELIMITED BY "-" INTO R1 R2.
           DISPLAY "T3 [" R1 "][" R2 "]".
      *> T4 - the delimiter is a slice of the numeric item: "1".
           MOVE SPACES TO R1 R2 R3.
           UNSTRING SRC2 DELIMITED BY NUM(1:1) INTO R1 R2 R3.
           DISPLAY "T4 [" R1 "][" R2 "][" R3 "]".
      *> T5 - CONCAT(GA(1:1) SRC3(1:2)) is "XAB" (alphanumeric), delimiter "B": R1 "XA".
           MOVE SPACES TO R1.
           UNSTRING FUNCTION CONCAT(GA(1:1) SRC3(1:2)) DELIMITED BY "B" INTO R1.
           DISPLAY "T5 [" R1 "]".
           STOP RUN.
