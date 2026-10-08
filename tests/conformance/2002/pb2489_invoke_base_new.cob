      *> kb/Work PB2489 - the standard class BASE instantiated itself
      *> (ISO 16.1, 16.2.1.2, 16.2.2.2, 14.9.39.4 GR10, 9.3.14.2).
      *> 16.1: "A standard class BASE shall be provided by the
      *> implementation." 16.2.1.2 GR1: "The New method allocates
      *> storage for an object, initializes its instance data ... and
      *> returns a reference to the created object." BASE's factory
      *> interface is New, so INVOKE BASE "NEW" is legal source and
      *> returns an object of class BASE. 16.2.2.2 GR1: FactoryObject
      *> "determines the class of the object and returns a reference
      *> to the factory object associated with that class"; 9.3.14.2
      *> gives a class ONE factory object, and 14.9.39.4 GR10 places a
      *> reference to it in the receiver of SET ... TO class-name.
      *> Expected values (derived from the rules, not from a run):
      *>   INVOKE BASE "NEW" RETURNING O (universal)  -> A:SET
      *>   INVOKE BASE "NEW" RETURNING OB (typed)     -> B:SET
      *>   O and OB are two created objects           -> AB:DISTINCT
      *>   SET FB TO BASE                             -> FB:SET
      *>   INVOKE FB "NEW" RETURNING OC               -> C:SET
      *>   INVOKE OB "FACTORYOBJECT" RETURNING FO     -> FO:SAME
      *>   INVOKE O "FACTORYOBJECT" RETURNING FO      -> FO2:SAME
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2489A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE.
       01 OB USAGE OBJECT REFERENCE BASE.
       01 OC USAGE OBJECT REFERENCE BASE.
       01 FB USAGE OBJECT REFERENCE FACTORY OF BASE.
       01 FO USAGE OBJECT REFERENCE FACTORY OF BASE.
       PROCEDURE DIVISION.
           INVOKE BASE "NEW" RETURNING O
           IF O = NULL DISPLAY "A:NULL" ELSE DISPLAY "A:SET" END-IF
           INVOKE BASE "NEW" RETURNING OB
           IF OB = NULL DISPLAY "B:NULL" ELSE DISPLAY "B:SET" END-IF
           IF O = OB DISPLAY "AB:SAME" ELSE DISPLAY "AB:DISTINCT"
           END-IF
           SET FB TO BASE
           IF FB = NULL DISPLAY "FB:NULL" ELSE DISPLAY "FB:SET" END-IF
           INVOKE FB "NEW" RETURNING OC
           IF OC = NULL DISPLAY "C:NULL" ELSE DISPLAY "C:SET" END-IF
           INVOKE OB "FACTORYOBJECT" RETURNING FO
           IF FO = FB DISPLAY "FO:SAME" ELSE DISPLAY "FO:OTHER"
           END-IF
           SET FO TO NULL
           INVOKE O "FACTORYOBJECT" RETURNING FO
           IF FO = FB DISPLAY "FO2:SAME" ELSE DISPLAY "FO2:OTHER"
           END-IF
           STOP RUN.
       END PROGRAM PB2489A.
