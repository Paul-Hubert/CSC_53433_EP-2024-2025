# The EvoSim documentation site

The source of the students' documentation website, built with [DocFX](https://dotnet.github.io/docfx/) 2.81. This file
is for whoever maintains the site; it is not part of the site.

| Folder or file | What it holds |
|---|---|
| `index.md`, `toc.yml` | the home page and the top navigation |
| `guides/` | how EvoSim works, one topic per page |
| `extending/` | how to extend it, one page per mechanism; `extension-points.md` is generated |
| `api/` | the API reference, generated at build time from the code (not committed) |
| `spec/` | the specification (`Docs/Interface`), copied at build time (not committed) |
| `images/` | screenshots |
| `template/public/` | `main.js` links rule ids (SENSE-01, V-08, "07 §4"…) to the specification; `main.css` |
| `tools/DocAudit/` | the documentation checker and generator (C#, compiled with the Roslyn that ships with Unity) |
| `tools/audit.ps1` | runs the checker |
| `build.ps1` | builds the site into `_site/` (not committed) |

## Install once

DocFX needs the .NET SDK. Both install under your user profile, without admin rights:

```powershell
$dir = "$env:LOCALAPPDATA\EvoSimDocs"
Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1 -OutFile "$dir\dotnet-install.ps1"
& "$dir\dotnet-install.ps1" -Channel 8.0 -InstallDir "$dir\dotnet" -NoPath
$env:DOTNET_ROOT = "$dir\dotnet"; & "$dir\dotnet\dotnet.exe" tool install docfx --tool-path "$dir\tools"
```

The scripts look there first, then on the `PATH`. The API pages are generated from the `EvoSim.*.csproj` files that
Unity writes at the repository root: open the project in Unity once on a fresh clone.

## Build, serve, check

```powershell
powershell -File Docs\Site\build.ps1            # → Docs\Site\_site
powershell -File Docs\Site\build.ps1 -Serve     # and serve it on http://localhost:8080
powershell -File Docs\Site\build.ps1 -Strict    # fail on any documentation issue or DocFX warning (CI)
```

`build.ps1` runs, in order: the documentation audit; the copy of the specification, the generated extension-points
page and the compilation of the pages' C# examples; `docfx metadata` (API pages from the code); the grouping of the API
table of contents by folder; `docfx build`. A full build takes about a minute.

The audit alone, for the inline documentation (`///` comments; the rules are in
[`extending/documenting-your-code.md`](extending/documenting-your-code.md)):

```powershell
powershell -File Docs\Site\tools\audit.ps1                                # everything
powershell -File Docs\Site\tools\audit.ps1 --only Runtime/Senses --max 50 # one folder of Assets/EvoSim
powershell -File Docs\Site\tools\audit.ps1 --same-code HEAD               # did a pass change anything but comments?
```

## Publish

`_site/` is a static site: copy it to any web server, GitHub Pages included. Its "View source" links point at the
GitHub repository and branch the site was built from (`origin` and the current branch).

## Writing a page

Readers are second-year students who know C# and a little Unity and are new to EvoSim.

- Plain English, short sentences, concise: a page is 80–220 lines. Say each thing once, link the rest.
- Concrete and accurate: real class and member names (check them in the code), reference values with units, menu
  paths with ▸, Inspector field names as the Inspector shows them.
- Front matter `uid: guide-<file>` or `extend-<file>`, one `#` title, short `##` sections, tables for settings and
  members, numbered lists for procedures, at most one or two small ```mermaid diagrams.
- API links: `<xref:EvoSim.Sense>`, or `[Read](xref:EvoSim.Sense.Read(EvoSim.Animal,EvoSim.SenseContext))` for a member
  (the uid is the documentation comment id without its prefix; a generic type's backtick is written `%60`).
- Specification: write rule ids (SENSE-01, V-08, T-ACT-09) or sections ("06 §2") in plain text; the site links them.
- Code: real code that compiles against the current API, under ~30 lines. A ```csharp block that declares a type is
  compiled by the build (`--examples`); mark a deliberate fragment ```cs instead.
- A guide page: what the part does and why; how it works; the main types; settings; what you see in the editor; links.
- An extending page: what you can change and when to choose this mechanism; the members to implement or override; a
  complete minimal example and where to put it; the rules to keep; how to test it; what to read next.
