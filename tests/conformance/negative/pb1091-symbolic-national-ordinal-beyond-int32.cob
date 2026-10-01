      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7.3 SR16 f2 (cite.py --check 12.3.7.3 "When the IN phrase is not
      *> specified, the ordinal position specified by integer-1 shall exist in the
      *> native national character set" -> OK 12.3.7.3 16) 2.). The native
      *> national set has 65,536 characters, so ordinal 4294967297 does not exist
      *> in it.
      *>
      *> kb/Work PB1091: the FOR NATIONAL arm of the silent skip. The binder read
      *> integer-1 with int.TryParse and `continue`d past the pair when that failed,
      *> so an ordinal beyond a 32-bit int bound NOTHING and drew no diagnostic;
      *> a later reference then said the name was "not defined". The ordinal now
      *> has one reader, which saturates, and the rule's own diagnostic names the
      *> clause. (The alphanumeric arm is pb1557-symbolic-ordinal-too-long.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1091SYMNAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SN IS 4294967297.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
