      *>--- conformance-case
      *> id: l1c40_no_automatic_alignment
      *> rules: [DOC-A.1-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.6.4 2) — no automatic alignment, no FILLER
      *> A group of DISPLAY and BINARY items with NO SYNCHRONIZED
      *> clause is seen by REDEFINES and group MOVEs exactly packed.
      *>
      *> THE RULES.
      *> §8.5.1.6.4 2): synchronization is accomplished "By
      *>   recognizing the appropriate natural boundaries and
      *>   organizing the data suitably without the use of the
      *>   SYNCHRONIZED clause." and "Each implementor who provides
      *>   for these special types of alignment shall describe the
      *>   effect of the implicit FILLER and the semantics of any
      *>   statement referencing these groups."
      *>   OK  §8.5.1.6.4 2)  (Item alignment for increased
      *>   object-code efficiency)  [both quotes]
      *> §13.18.55.4 GR10: "An implementor may optionally specify
      *>   automatic alignment for any internal data representations"
      *>   OK  §13.18.55.4 10)  (General rules)
      *> Annex A.1 item 7 makes this alignment optional. The
      *>   documented choice, docs/CONFORMANCE.md DOC-A.1-7, is "Not
      *>   provided": every item sits immediately after its
      *>   predecessor with no implicit FILLER, "so no statement sees
      *>   a group differently because of alignment".
      *> Widths from DOC-A.1-205: BINARY of 5-9 digits = 4 bytes,
      *>   of 3-4 digits = 2 bytes.
      *> §13.18.44.1: "The REDEFINES clause allows the same computer
      *>   storage area to be described by different data description
      *>   entries."
      *> §14.9.25.4 GR4: a group move "is treated exactly as if it
      *>   were an alphanumeric to alphanumeric elementary move"
      *>   OK  §14.9.25.4 4)  (General rules)
      *> §14.6.8.5: the data is "aligned at the leftmost character
      *>   position in the data item with space fill or truncation to
      *>   the right"
      *>   OK  §14.6.8.5  (Receiving data items of categories
      *>   alphabetic, alphanumeric, ...)
      *>
      *> DERIVATION. G = GA X(1) @1, GB S9(9) BINARY (4) @2-5,
      *>   GC X(1) @6, GD S9(4) BINARY (2) @7-8, GE X(1) @9:
      *>   9 bytes, no FILLER. An implementor aligning each binary
      *>   item on its own width would put GB @5-8, GC @9, GD @11-12,
      *>   GE @13 (13 bytes), and every line below would change.
      *> HC=C HE=E -- H REDEFINES G as X(1)/X(4)/X(1)/X(2)/X(1):
      *>   HC is byte 6 = GC "C", HE is byte 9 = GE "E".
      *> R6=C R9=E -- MOVE G TO R (X(12)) places the 9 bytes
      *>   leftmost, so R(6:1) = "C" and R(9:1) = "E".
      *> TAIL=[   ] -- the group is 9 bytes, so R positions 10-12
      *>   are the space fill of §14.6.8.5.
      *> GA=P GC=U GE=X -- MOVE "PQRSTUVWXYZ" TO G is an
      *>   alphanumeric move into the 9-byte group, truncated on the
      *>   right: byte 1 "P", byte 6 "U", byte 9 "X".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C40A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GA PIC X VALUE "A".
          05 GB PIC S9(9) BINARY VALUE ZERO.
          05 GC PIC X VALUE "C".
          05 GD PIC S9(4) BINARY VALUE ZERO.
          05 GE PIC X VALUE "E".
       01 H REDEFINES G.
          05 HA PIC X.
          05 FILLER PIC X(4).
          05 HC PIC X.
          05 FILLER PIC X(2).
          05 HE PIC X.
       01 R PIC X(12).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HC=" HC " HE=" HE
           MOVE G TO R
           DISPLAY "R6=" R(6:1) " R9=" R(9:1)
           DISPLAY "TAIL=[" R(10:3) "]"
           MOVE "PQRSTUVWXYZ" TO G
           DISPLAY "GA=" GA " GC=" GC " GE=" GE
           STOP RUN.
