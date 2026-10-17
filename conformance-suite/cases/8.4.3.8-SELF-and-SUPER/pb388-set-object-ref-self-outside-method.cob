      *>--- conformance-case
      *> id: pb388-set-object-ref-self-outside-method
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.4.3.8.3 SR1 — "This identifier format may be specified
      *> only in a method definition." SELF is the predefined object
      *> reference for the object the CURRENT METHOD is executing on
      *> (§8.4.3.8.1), and a PROGRAM has no such object, so
      *> SET identifier TO SELF is refused in a program at every edition
      *> that has the format (COBOL-2002 introduced it).
      *>
      *> ⛔ AND THE CITATION IS THE OTHER HALF OF WHAT THIS PINS. The
      *> message cited §14.9.39.3 SR12 c), which answers a DIFFERENT
      *> question — SR12 governs a receiver described with an
      *> object-class-name, and its c)3./c)4. are about the FACTORY vs
      *> INSTANCE placement of a method that exists. WS-OBJ below is a
      *> UNIVERSAL object reference, which SR12 never reaches at all.
      *>
      *> ⛔ THE SECOND ARM OF THE ELISION SWEEP (kb/Work PB388). This
      *> message read `SET … TO SELF` while targetRefs was in scope, so
      *> the renderer's ASCII transliteration showed `SET . TO SELF`.
      *> Its twin is pb388-set-pointer-self-sender.cob, which is the
      *> SAME defect one general format over (Format 7's carrier arm,
      *> COBOLNET0869). Two arms, one repair: the receiving list is
      *> named through SetFormatSelection.Written at both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB388-SET-OBJ-SELF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-OBJ USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-OBJ TO SELF.
           GOBACK.
