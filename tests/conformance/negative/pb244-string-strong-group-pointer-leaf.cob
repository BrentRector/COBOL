      *> reject-at: 2002 2014 2023
      *> ISO 14.9.43.3 SR1 (kb/Work PB244): "all identifiers, except identifier-4, shall be described implicitly or
      *> explicitly as usage display or national." 8.5.2.1 gives only an ALPHANUMERIC group item the usage of
      *> display; the class and category of a strongly-typed group are its type-name, so its usage is read off its
      *> elementary items and a USAGE POINTER leaf is neither display nor national. The program used to compile and
      *> abort at run time in the Tier-C whole-group island; it is refused at bind (COBOLNET1626).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NSTRPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GPT IS TYPEDEF STRONG.
          05 GA PIC X(3).
          05 GP USAGE POINTER.
       01 WS-GP TYPE GPT.
       01 WS-DST PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           STRING WS-GP DELIMITED BY SIZE INTO WS-DST
           STOP RUN.
