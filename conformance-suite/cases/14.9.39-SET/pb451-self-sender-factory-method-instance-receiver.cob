*>--- conformance-case
*> id: pb451-self-sender-factory-method-instance-receiver
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 c)3.: "if the data item referenced by identifier-3 is
      *> described without a FACTORY phrase, the method containing the SET statement shall be
      *> defined in the instance definition of its containing class."  Inside a FACTORY method
      *> SELF is the class's FACTORY object, and a receiver described with a bare
      *> object-class-name holds an INSTANCE object (§13.18.60.4 GR22 d)1.b.).  SR12 c)2. is
      *> deliberately SATISFIED here — the containing class IS the receiver's class — so the
      *> class-relation test cannot be what rejects it.
      *> This is the arm that used to escape the binder entirely and die in the BACKEND with a
      *> Roslyn CS0030 naming a synthesized type that appears nowhere in the program
      *> (kb/Work PB451); its interface twin, SR10 d), had been repaired one arm over.
      *> The conforming pair is the FMK / MK methods of
      *> conformance:2002/pb451_set_format5_self_sender.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N5C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N5C "MK".
           STOP RUN.
       END PROGRAM PB451N5.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N5C.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG".
       END METHOD TAG.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB451N5C.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD MK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO".
       END METHOD WHO.
       END OBJECT.
       END CLASS PB451N5C.
