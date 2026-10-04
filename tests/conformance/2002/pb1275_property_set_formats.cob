      *> kb/Work PB1275 -- an object property of class object or pointer
      *> in the statements that store an object reference or a pointer.
      *> ISO 8.4.3.9.3 SR5/SR6: the property "may be specified wherever a
      *> data item with that description would be valid as a sending
      *> item" / "as a receiving item"; 8.4.3.9.4 GR1 (sending only: the
      *> GET runs), GR2 (receiving only: the SET runs, the GET does NOT)
      *> and GR3 (both: GET before, SET after).  13.18.42.4 GR1/GR2 give
      *> the implicit GET / SET methods the PROPERTY clause defines.
      *> Every line was refused COBOLNET0843 "outside the classified store
      *> taxonomy"; each expected value is DERIVED from the rules cited:
      *>  1 SAME   SET R OF A TO A (Format 5, R a receiving property, GR2)
      *>           then the relation R OF A = A (sending, GR1): the same
      *>           object (8.8.4.2.15).
      *>  2 SAME   SET B TO R OF A -- the property as the Format 5 SENDER.
      *>  3 SAME   SET Q OF A TO ADDRESS OF W (Format 7 receiver), then
      *>           SET P2 TO Q OF A (Format 7 sender): P2 holds W's address.
      *>  4 SAME   an object-reference property typed P1275D as the Format 5
      *>           receiver and sender (SET INNER OF A TO D / SET D2 TO
      *>           INNER OF A): D2 and D reference the same object.
      *>  5 SAME   SET Q OF A UP BY 2 (Format 10 reads and writes its
      *>           receiver: 8.4.3.9.4 GR3) equals ADDRESS OF W moved up by
      *>           2 bytes (14.9.39.4 GR20).
      *>  6 ALLOC  ALLOCATE 8 CHARACTERS RETURNING Q OF A stores the
      *>           address of the new storage (14.9.3.4 GR4): not NULL.
      *>  7 FREED  FREE Q OF A releases it and sets the pointer to NULL
      *>           (14.9.15.4 GR1 a)) -- read and written: 8.4.3.9.4 GR3.
      *>  8 ABCDEFGH  STRING ... INTO S OF A, where S is PROPERTY WITH NO
      *>           GET: identifier-3 "is the receiving operand" (14.9.43.3
      *>           SR10), so the property is used only as a receiving item
      *>           (GR2), SR3's GET is not required, and the SET stores the
      *>           eight transferred characters; SHOWS displays S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1275SF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1275C
           CLASS P1275D
           PROPERTY R
           PROPERTY Q
           PROPERTY INNER
           PROPERTY S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  USAGE OBJECT REFERENCE P1275C.
       01 B  USAGE OBJECT REFERENCE.
       01 D  USAGE OBJECT REFERENCE P1275D.
       01 D2 USAGE OBJECT REFERENCE P1275D.
       01 P2 USAGE POINTER.
       01 W  PIC X(8) VALUE "TARGET".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P1275C "NEW" RETURNING A.
           SET R OF A TO A.
           IF R OF A = A DISPLAY "1=SAME" ELSE DISPLAY "1=DIFF".
           SET B TO R OF A.
           IF B = A DISPLAY "2=SAME" ELSE DISPLAY "2=DIFF".
           SET Q OF A TO ADDRESS OF W.
           SET P2 TO Q OF A.
           IF P2 = ADDRESS OF W DISPLAY "3=SAME" ELSE DISPLAY "3=DIFF".
           INVOKE P1275D "NEW" RETURNING D.
           SET INNER OF A TO D.
           SET D2 TO INNER OF A.
           IF D2 = D DISPLAY "4=SAME" ELSE DISPLAY "4=DIFF".
           SET Q OF A UP BY 2.
           SET P2 TO ADDRESS OF W.
           SET P2 UP BY 2.
           IF Q OF A = P2 DISPLAY "5=SAME" ELSE DISPLAY "5=DIFF".
           ALLOCATE 8 CHARACTERS RETURNING Q OF A.
           IF Q OF A NOT = NULL DISPLAY "6=ALLOC"
           ELSE DISPLAY "6=NULL".
           FREE Q OF A.
           IF Q OF A = NULL DISPLAY "7=FREED" ELSE DISPLAY "7=LIVE".
           STRING "ABCD" "EFGH" DELIMITED BY SIZE INTO S OF A.
           DISPLAY "8=" WITH NO ADVANCING.
           INVOKE A "SHOWS".
           STOP RUN.
       END PROGRAM PB1275SF.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1275C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS P1275D.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R     USAGE OBJECT REFERENCE PROPERTY.
       01 Q     USAGE POINTER PROPERTY.
       01 INNER USAGE OBJECT REFERENCE P1275D PROPERTY.
       01 S     PIC X(8) PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWS.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY S.
       END METHOD SHOWS.
       END OBJECT.
       END CLASS P1275C.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1275D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS P1275D.
