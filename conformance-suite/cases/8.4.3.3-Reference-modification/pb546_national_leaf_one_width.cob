*>--- conformance-case
*> id: pb546_national_leaf_one_width
*> rules: [GR-8.4.3.3.4-3, GR-13.18.60.4-8, SR-14.8.2.2-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB546 - ONE unit reader: how many bytes a position occupies is a
      *> function of the USAGE (ISO 13.18.60.4 GR8; design D-N1: two bytes, UTF-16),
      *> so every category a usage-national item can have (13.18.60.3 SR12: boolean,
      *> national, national-edited, numeric, numeric-edited) occupies two bytes a
      *> position, whole or reference-modified (8.4.3.3.4 GR3: the slice is operated
      *> upon as if national of the same size). BYTE-LENGTH is bytes (15.14.4 r1);
      *> LENGTH of a boolean item counts boolean positions (15.50.4 r1); an
      *> alphanumeric group counts a national position as the two bytes it
      *> occupies (15.50.4 r3), so A sits at bytes 5-6 of G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB546P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BN  PIC 1(4) USAGE NATIONAL VALUE B"1010".
       01 NN  PIC 9(4) USAGE NATIONAL VALUE 1234.
       01 NED PIC ZZ9 USAGE NATIONAL.
       01 NE  PIC NBN VALUE N"AB".
       01 G.
          05 N PIC N(2) USAGE NATIONAL.
          05 A PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"AB" TO N
           MOVE "YZ" TO A
           DISPLAY "BYTE-LENGTH-BN=" FUNCTION BYTE-LENGTH(BN)
           DISPLAY "BYTE-LENGTH-BN-1-2=" FUNCTION BYTE-LENGTH(BN(1:2))
           DISPLAY "LENGTH-BN-1-2=" FUNCTION LENGTH(BN(1:2))
           DISPLAY "BYTE-LENGTH-NN=" FUNCTION BYTE-LENGTH(NN)
           DISPLAY "BYTE-LENGTH-NN-1-2=" FUNCTION BYTE-LENGTH(NN(1:2))
           DISPLAY "BYTE-LENGTH-NED=" FUNCTION BYTE-LENGTH(NED)
           DISPLAY "BYTE-LENGTH-NED-1-2=" FUNCTION BYTE-LENGTH(NED(1:2))
           DISPLAY "BYTE-LENGTH-NE=" FUNCTION BYTE-LENGTH(NE)
           DISPLAY "BYTE-LENGTH-NE-1-2=" FUNCTION BYTE-LENGTH(NE(1:2))
           DISPLAY "LENGTH-G=" FUNCTION LENGTH(G)
           DISPLAY "BYTE-LENGTH-G=" FUNCTION BYTE-LENGTH(G)
           DISPLAY "A-AT-5-6=[" G(5:2) "]"
           STOP RUN.
