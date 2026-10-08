      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2518 - ISO/IEC 1989:2023 section 13.18.44.3 SR10: "The entries giving the new descriptions of the
      *> storage area shall follow the entries defining the area of data-name-2, without intervening entries that
      *> define new storage areas."
      *>   cite.py --check 13.18.44.3 "without intervening entries that define new storage areas" -> OK  10)
      *> C defines a new storage area between A and B, so B REDEFINES A reaches past it. COBOLNET2739 (the entry-level
      *> rule family, DataBinder.RedefinesEntry.cs). The positive twin is
      *> conformance/85/pb2518_redefines_only_redefinitions_between.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2518N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(2).
          05 C PIC X.
          05 B REDEFINES A PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
