      *> PB63 - FUNCTION EXCEPTION-LOCATION / -N (15.30.3 r2b / 15.31.3 r2b): three parts. 1. "The name of the
      *> runtime element as specified in the FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the function,
      *> method, or program containing the statement" - a statement inside a METHOD names the METHOD-ID (M=,
      *> before PB63 the CLASS-ID). 2. the procedure-name: a) no paragraph-name and no section-name -> ";" and a
      *> space (A= - an EMPTY field; before PB63 a display placeholder), b) a paragraph-name, plus " OF section"
      *> when within a section (B=), c) a section-name and no paragraph-name -> the section-name alone (C=).
      *> 3. the implementor-defined line identifier - COBOL.NET's is the resultant-text line (CONFORMANCE.md);
      *> with no COPY it is the source line: the RAISE statements sit on lines 39 and 42 of this file and
      *> lines 67 and 70 (the method's). Case a) is the METHOD's leading sentence: a procedure division with
      *> sections is 14.2.1 Format 1, which admits no sentence before its first section (kb/Work PB1146), so a
      *> statement with no paragraph-name and no section-name exists only in a Format 2 body like SHOWLOC's.
      *> Also pinned here (the FMT-15.30.2 / FMT-15.31.2 / FMT-15.32.2 / FMT-15.33.2 forms the sweep found
      *> fixed): keyword-omitted EXCEPTION-LOCATION under FUNCTION ALL INTRINSIC (8.4.3.2.3 SR2), a
      *> reference-modified EXCEPTION-LOCATION-N (1:7) - a zero-argument NATIONAL function, so 8.4.3.3.3 SR2
      *> admits the ref-mod (KO/RM=), FUNCTION EXCEPTION-STATEMENT as a WRITE ... FROM sending operand
      *> (WR=), FUNCTION EXCEPTION-STATUS as an INSPECT subject (TALLY=2 - EC-USER-L has two hyphens) and as
      *> an INITIALIZE ... REPLACING sending operand (INI=).
       >>TURN EC-USER CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63LOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB63LOCCL
           FUNCTION ALL INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "pb63loc.txt" ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD OUTF.
       01 OREC PIC X(20).
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0.
       01 WS-D PIC X(12).
       01 IREC PIC X(20).
       PROCEDURE DIVISION.
       ONLY-SECT SECTION.
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "C=[" FUNCTION EXCEPTION-LOCATION "]".
       IN-PARA.
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "B=[" FUNCTION EXCEPTION-LOCATION "]".
           DISPLAY "N=[" DISPLAY-OF(FUNCTION EXCEPTION-LOCATION-N) "]".
           DISPLAY "KO=[" EXCEPTION-LOCATION "]".
           DISPLAY "RM=[" DISPLAY-OF(FUNCTION EXCEPTION-LOCATION-N (1:7)) "]".
           INVOKE PB63LOCCL "SHOWLOC".
           OPEN OUTPUT OUTF.
           WRITE OREC FROM FUNCTION EXCEPTION-STATEMENT.
           CLOSE OUTF.
           OPEN INPUT OUTF.
           READ OUTF INTO IREC AT END CONTINUE END-READ.
           CLOSE OUTF.
           DISPLAY "WR=[" FUNCTION TRIM(IREC) "]".
           INSPECT FUNCTION EXCEPTION-STATUS TALLYING WS-N FOR ALL "-".
           DISPLAY "TALLY=" WS-N.
           INITIALIZE WS-D REPLACING ALPHANUMERIC DATA BY FUNCTION EXCEPTION-STATUS.
           DISPLAY "INI=[" WS-D "]".
           STOP RUN.
       END PROGRAM PB63LOC.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB63LOCCL.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWLOC.
       PROCEDURE DIVISION.
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "A=[" FUNCTION EXCEPTION-LOCATION "]".
       M-PARA.
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "M=[" FUNCTION EXCEPTION-LOCATION "]".
       END METHOD SHOWLOC.
       END FACTORY.
       END CLASS PB63LOCCL.
