[to Overview](../README.md)

---
# Current Issues
## Non-Destructive
The program is *non-destructive* aside from the ⸉clear⸉ function in ⟨0. settings⟩. Your information will not be deleted. If you use object composition you will need to *manually* remove files.

## Parsing
Parsing is generally buggy when it comes to **information** fields. *Parsing will work as expected for all non-information fields.* Expect issues specifically with nested information. Because the program is non-destructive this is an issue only when viewing information fields from the CLI and does not result in any issues with your notes.

## Information Containing Keywords
Information fields which contain keywords like `-`, `->`, `*`, etc. will fail or cause unexpected results.