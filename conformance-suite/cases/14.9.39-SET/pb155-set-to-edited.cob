      *>--- conformance-case
      *> id: pb155-set-to-edited
      *> rules: [SR-14.9.39.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.8.1.1 via 13.18.38.3 r7's index-name window: the window
      *> admits an INDEX-NAME beside the ordinary numeric operands - it
      *> does not suspend the class screen. R29 moved SET / PERFORM
      *> VARYING / compound subscripts and relation operands to the
      *> window context and the 8.8.1.1 screen was never widened to it,
      *> so SET IX TO <alphanumeric-edited> silently digit-decoded
      *> under STRICT while ADD drew 0844 (kb/Work PB155's sweep).
      *> kb/Work PB212: the refusal is now 14.9.39.3 SR2's own - XE is
      *> neither identifier-2 (class index) nor numeric (COBOLNET2326).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC XXBXX VALUE "AB CD".
       01 T.
          05 E OCCURS 3 INDEXED BY IX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO XE
           STOP RUN.
