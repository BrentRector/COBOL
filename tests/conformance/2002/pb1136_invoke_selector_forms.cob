      *> ISO 1989:2023 §14.9.23 — the INVOKE method SELECTOR in every class the standard admits (kb/Work PB1136).
      *>   SR2: "Literal-1 shall be of class alphanumeric or national and shall not be a zero-length literal."
      *>   SR8: "Identifier-2 shall reference an alphanumeric or national data item."
      *>   GR2 a): literal-1 or the content of identifier-2 "is the name of the method to be invoked as described in
      *>   8.3.2.2, User-defined words" — so every selector below names the method GREET or GETNAME.
      *> Every expected line is DERIVED from the standard, not captured:
      *>   1 HI        identifier-2 of category NATIONAL (PIC N(5) VALUE N"GREET") through the universal U — SR8's
      *>               national half (it was refused "a later refinement").
      *>   2 HI        identifier-2 a NATIONAL GROUP (GROUP-USAGE NATIONAL is class and category national,
      *>               §13.18.29.4 GR2 b)) holding "GREET".
      *>   3 HI        literal-1 hexadecimal-NATIONAL: §8.3.3.5.4 GR4 gives one national character per
      *>               hex-character-sequence (four digits here, D-N1), so NX"00470052004500450054" is GREET — the
      *>               method-name decoder read it as alphanumeric hex (" G R E E T") and refused it.
      *>   4 ACCOUNT   the same NX literal-1 as the INLINE form's method name (§8.4.3.4.3 SR3 holds it to
      *>               §14.9.23's syntax rules), NX"004700450054004E0041004D0045" = GETNAME; the PIC X(8) result
      *>               "ACCOUNT " is DISPLAYed whole.
      *>   5 ACCOUNT   an alphanumeric concatenation expression as the INLINE method name: §8.8.3.3 GR3 — it "may
      *>               be used anywhere a literal of that class may be used" ("GET" & "NAME" = GETNAME).
      *>   6 HI        a concatenation as the INVOKE statement's literal-1 ("GR" & "EET"), the twin of 5.
      *>   7 HI        identifier-2 an ALPHANUMERIC GROUP ("class and category alphanumeric", §8.5.2.1) holding
      *>               "GREET" - its content is the group's character positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136SF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1136C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE PB1136C.
       01 U  USAGE OBJECT REFERENCE.
       01 MN PIC N(5) VALUE N"GREET".
       01 MG GROUP-USAGE NATIONAL.
          05 MG1 PIC N(3) VALUE N"GRE".
          05 MG2 PIC N(2) VALUE N"ET".
       01 MA.
          05 MA1 PIC X(2) VALUE "GR".
          05 MA2 PIC X(3) VALUE "EET".
       01 W  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1136C "NEW" RETURNING O.
           SET U TO O.
           DISPLAY "1=" WITH NO ADVANCING.
           INVOKE U MN.
           DISPLAY "2=" WITH NO ADVANCING.
           INVOKE U MG.
           DISPLAY "3=" WITH NO ADVANCING.
           INVOKE O NX"00470052004500450054".
           MOVE O :: NX"004700450054004E0041004D0045" TO W.
           DISPLAY "4=" W.
           MOVE O :: "GET" & "NAME" TO W.
           DISPLAY "5=" W.
           DISPLAY "6=" WITH NO ADVANCING.
           INVOKE O "GR" & "EET".
           DISPLAY "7=" WITH NO ADVANCING.
           INVOKE U MA.
           STOP RUN.
       END PROGRAM PB1136SF.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI".
       END METHOD GREET.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB1136C.
