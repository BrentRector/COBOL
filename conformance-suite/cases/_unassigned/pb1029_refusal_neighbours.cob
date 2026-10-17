      *>--- conformance-case
      *> id: pb1029_refusal_neighbours
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1029 - the LEGAL neighbours of the operand refusals that used to compile silently
      *> and abort the run unit. ISO 14.9.43.3 SR2 bars only a figurative constant that BEGINS WITH
      *> THE WORD ALL; a plain figurative is legal and "refers to an implicit one character data
      *> item" (14.9.43.4 GR2), so STRING "AB" SPACE "CD" writes AB CD into the first five positions
      *> and leaves the rest of WS-A as it was. ISO 14.9.22.3 SR3 likewise lets an INSPECT literal be
      *> a figurative (one character) but not an ALL figurative: FOR ALL SPACE tallies the two
      *> spaces of "AB  CD".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1029P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(10) VALUE "**********".
       01 WS-B PIC X(6) VALUE "AB  CD".
       01 WS-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED BY SIZE
                  SPACE DELIMITED BY SIZE
                  "CD" DELIMITED BY SIZE
                  INTO WS-A
           END-STRING
           DISPLAY WS-A
           INSPECT WS-B TALLYING WS-N FOR ALL SPACE
           DISPLAY WS-N
           STOP RUN.
