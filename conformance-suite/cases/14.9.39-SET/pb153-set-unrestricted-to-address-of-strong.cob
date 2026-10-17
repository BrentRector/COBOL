      *>--- conformance-case
      *> id: pb153-set-unrestricted-to-address-of-strong
      *> rules: [SR-14.9.39.3-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.3 SR19, THIRD SENTENCE - THE SENDER ARM.
      *> (It is an UNNUMBERED continuation paragraph of rule 19),
      *> NOT SR20: rendered folio 736 / PDF p766, 20) is FORMAT 8's
      *> function-pointer rule. This comment said SR20 - kb/Work
      *> PB450 corrected it.)
      *> "If identifier-6
      *> references a restricted data-pointer, either identifier-5
      *> shall reference a data-pointer restricted to the same type or
      *> data-name-1 shall be a typed item of the type to which
      *> identifier-6 is restricted."
      *> ADDRESS OF a strongly-typed group IS a restricted
      *> data-pointer - 8.4.3.11.4 GR2, and Annex D.9.2.2 item 2 lists
      *> it as one of the model's TWO sources. THIS ARM NEEDS NO
      *> GRAMMAR AT ALL and was violable from the day strong TYPEDEF
      *> landed. kb/Work PB153.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 V TYPE TPT.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET Q TO ADDRESS OF V.
           STOP RUN.
