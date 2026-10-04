      *> reject-at: 2014 2023
      *> kb/Work PB1407. ISO 8.4.3.11.3 SR6: "Identifier-1 shall not reference a dynamic-length elementary item, an
      *> element of a dynamic-capacity table, an item subordinate to a dynamic-capacity table, or an item subordinate to a
      *> group that contains a dynamic-length elementary item." Each shape below is refused COBOLNET2785: the dynamic-length
      *> item (D1), its sibling under the group that contains it (D2), an element of a dynamic-capacity table (TE) and an
      *> item under a dynamic-capacity table (TG-A). A fixed item beside them (F1) is addressed in the same statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DR.
          05 D1 PIC X DYNAMIC LENGTH.
          05 D2 PIC X(4).
       01 DT.
          05 TE PIC X(4) OCCURS DYNAMIC CAPACITY IN TC.
          05 TG OCCURS DYNAMIC CAPACITY IN TGC.
             10 TG-A PIC X(4).
       01 F1 PIC X(4).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF D1
           SET P TO ADDRESS OF D2
           SET P TO ADDRESS OF TE(1)
           SET P TO ADDRESS OF TG-A(1)
           SET P TO ADDRESS OF F1
           STOP RUN.
