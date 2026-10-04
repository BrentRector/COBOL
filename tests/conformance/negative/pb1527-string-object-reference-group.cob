      *> reject-at: 2002 2014 2023
      *> ISO 14.9.43.3 SR1 (kb/Work PB1527): "all identifiers, except identifier-4, shall be described implicitly or
      *> explicitly as usage display or national." 8.5.2.1 gives only an ALPHANUMERIC group item the usage of display;
      *> the class and category of a strongly-typed group are its type-name, so its usage is read off its elementary
      *> items, and a USAGE OBJECT REFERENCE leaf is neither display nor national. The program used to compile and
      *> abort at run time in the Tier-C whole-group island; it is refused at bind (COBOLNET1626). A group holding a
      *> pointer or object reference is legal only inside a STRONG type (13.18.60.3 SR14), so this is the one shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1527NSTR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1527NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 A PIC X(3).
          05 R USAGE OBJECT REFERENCE PB1527NC.
       01 G TYPE TG.
       01 XX PIC X(20).
       PROCEDURE DIVISION.
       MAIN.
           STRING G DELIMITED BY SIZE INTO XX
           STOP RUN.
       END PROGRAM PB1527NSTR.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1527NC.
       OBJECT.
       END OBJECT.
       END CLASS PB1527NC.
