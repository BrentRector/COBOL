*> reject-at: 85
*> THE NEGATIVE BELOW THE INTRODUCING EDITION (kb/Work PB1185). ISO 14.9.48.4 GR7 makes a figurative
*> delimiter a NATIONAL literal when identifier-1 is national, and 8.3.3.6.4 GR6 then takes its
*> HIGH-VALUE / LOW-VALUE from the national program collating sequence -
*> conformance:2002/pb1185_figurative_delimiter_national_pcs is the positive. National data and the
*> PROGRAM COLLATING SEQUENCE ... FOR NATIONAL phrase are COBOL-2002 introductions, so the same statement
*> over a national sender is not conforming COBOL-85 and the edition gate refuses it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1185N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-SRC PIC N(5) VALUE N"ABCDE".
       01 N-1   PIC N(5).
       01 N-2   PIC N(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING N-SRC DELIMITED BY LOW-VALUE INTO N-1 N-2.
           DISPLAY N-1 N-2.
           STOP RUN.
