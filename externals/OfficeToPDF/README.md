# OfficeToPDF (bundled)

Command-line converter used by Canon Print Bridge to turn Office documents
(`.docx/.doc/.rtf`, and other Office formats) into PDF on the Win11 host before
they enter the print queue. It drives the **installed** Microsoft Office via COM,
so Office must be present on the machine.

Usage: `OfficeToPDF.exe "input.docx" "output.pdf"` (exit code 0 = success).

## Provenance

- Upstream: https://github.com/cognidox/OfficeToPDF (Apache-2.0, see `LICENSE.md`).
- Built from commit `fac2493c1a72460399c4c31a74f32345ebbf3b66`.
- Single-file exe (dependencies embedded via Costura.Fody); only `OfficeToPDF.exe`
  and `OfficeToPDF.exe.config` are needed at runtime.

## Local build patch

The upstream project references Microsoft Project and Visio interop assemblies,
which aren't installed on this host (only Word/Excel/PowerPoint/Outlook/Publisher).
To build, the Project and Visio converters were removed:

- `OfficeToPDF.csproj`: dropped the `Microsoft.Office.Interop.MSProject` and
  `Microsoft.Office.Interop.Visio` references and excluded `ProjectConverter.cs` /
  `VisioConverter.cs` from compilation.
- `Program.cs`: removed the `.mpp` (Project) and `.vsd*/.vdx/.svg/.emf/...` (Visio)
  dispatch cases.

Word/Excel/PowerPoint/Outlook/Publisher/XPS conversion is unaffected. To rebuild:
`nuget restore` then MSBuild `-p:Configuration=Release`.
