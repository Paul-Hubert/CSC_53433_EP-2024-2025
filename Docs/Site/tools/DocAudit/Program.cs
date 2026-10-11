using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EvoSim.DocAudit
{
    /// <summary>
    /// Checks the inline documentation of the EvoSim code (Docs/Site/README.md): compiles every EvoSim assembly from the
    /// project files Unity writes (EvoSim.*.csproj), reports the compiler's documentation warnings and every declaration
    /// without a doc comment, and can write the XML documentation files, the extension-points page of the site, and a
    /// proof that a documentation pass changed comments only.
    /// </summary>
    /// <remarks>
    /// <code>
    /// DocAudit [--root DIR] [--only Runtime/Core,Editor] [--max N]   report missing and malformed documentation
    ///          [--no-report]                                         skip the report (to only write the outputs below)
    ///          [--xml DIR]                                           write EvoSim.*.dll and EvoSim.*.xml
    ///          [--inventory FILE]                                    write the extension-points page (Markdown)
    ///          [--api-toc DIR]                                       group DocFX's API pages by folder (toc.yml, index.md)
    ///          [--spec DIR]                                          copy the specification (Docs/Interface) for the site
    ///          [--same-code GITREF]                                  check that only comments and spaces changed since GITREF
    ///          [--examples DIR,DIR]                                  compile the complete C# examples of the site's pages
    /// </code>
    /// Exit code 0: nothing to report; 1: documentation issues or code changes; 2: the code does not compile.
    /// </remarks>
    static class Program
    {
        /// <summary>The EvoSim assemblies, in dependency order (the project files at the repository root).</summary>
        static readonly string[] AssemblyNames =
        {
            "EvoSim.Runtime", "EvoSim.Http", "EvoSim.Samples", "EvoSim.Testing", "EvoSim.Editor", "EvoSim.Tests.EditMode",
            "EvoSim.Tests.PlayMode",
        };

        /// <summary>The compiler warnings about documentation comments.</summary>
        static readonly HashSet<string> DocCodes = new HashSet<string>
        {
            "CS1570", "CS1571", "CS1572", "CS1573", "CS1574", "CS1580", "CS1581", "CS1584", "CS1587", "CS1589", "CS1590",
            "CS1591", "CS1592", "CS1658", "CS1710", "CS1711", "CS1712", "CS1723", "CS1734", "CS1735",
        };

        /// <summary>Folder of the EvoSim code, relative to the repository root.</summary>
        const string CodeRoot = "Assets/EvoSim/";

        /// <summary>One thing to fix: where, which check, and what.</summary>
        sealed class Issue
        {
            public string File, Code, Text;
            public int Line;
            public override string ToString() => $"{File}({Line}): {Code} {Text}";
        }

        /// <summary>One assembly: its sources, references and compilation.</summary>
        sealed class Project
        {
            public string Name;
            public List<string> Sources = new List<string>();
            public List<string> References = new List<string>();
            public List<string> ProjectReferences = new List<string>();
            public string[] Defines = Array.Empty<string>();
            public LanguageVersion Language = LanguageVersion.CSharp9;
            public CSharpCompilation Compilation;
        }

        static int Main(string[] args)
        {
            string root = Directory.GetCurrentDirectory(), xmlOut = null, inventory = null, sameCode = null, apiToc = null, spec = null, examples = null;
            var only = new List<string>();
            int max = 400;
            bool report = true;
            for (int i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
                switch (args[i])
                {
                    case "--root": root = Path.GetFullPath(Next()); break;
                    case "--only": only.AddRange(Next().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().Replace('\\', '/').TrimEnd('/'))); break;
                    case "--max": max = int.Parse(Next()); break;
                    case "--no-report": report = false; break;
                    case "--xml": xmlOut = Path.GetFullPath(Next()); break;
                    case "--inventory": inventory = Path.GetFullPath(Next()); break;
                    case "--same-code": sameCode = Next(); break;
                    case "--api-toc": apiToc = Path.GetFullPath(Next()); break;
                    case "--spec": spec = Path.GetFullPath(Next()); break;
                    case "--examples": examples = Next(); break;
                    default: Console.Error.WriteLine($"unknown option {args[i]}"); return 2;
                }
            }

            var projects = LoadProjects(root);
            var errors = projects.SelectMany(p => p.Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).ToList();
            if (errors.Count > 0)
            {
                Console.WriteLine($"the code does not compile ({errors.Count} errors); first ones:");
                foreach (var e in errors.Take(10)) Console.WriteLine("  " + e);
                return 2;
            }

            var issues = new List<Issue>();
            if (report)
            {
                foreach (var p in projects) issues.AddRange(Audit(p, root));
                issues = issues.Where(x => InScope(x.File, only)).OrderBy(x => x.File, StringComparer.Ordinal).ThenBy(x => x.Line).ToList();
                Report(issues, only, max);
            }

            if (xmlOut != null) WriteXml(projects, xmlOut);
            if (inventory != null) WriteInventory(projects, inventory);
            if (apiToc != null) WriteApiToc(projects, apiToc, root);
            if (spec != null) WriteSpec(root, spec);
            int changed = sameCode != null ? CompareCode(root, sameCode, projects, only) : 0;
            int broken = examples != null ? CheckExamples(projects, examples, root) : 0;
            return issues.Count > 0 || changed > 0 || broken > 0 ? 1 : 0;
        }

        // ---------------------------------------------------------------- projects

        /// <summary>Reads the EvoSim project files and compiles them in dependency order, plus the exercise stubs.</summary>
        static List<Project> LoadProjects(string root)
        {
            var byName = new Dictionary<string, Project>();
            var list = new List<Project>();
            foreach (var name in AssemblyNames)
            {
                string file = Path.Combine(root, name + ".csproj");
                if (!File.Exists(file)) throw new FileNotFoundException($"{file} is missing: open the project in Unity once (it writes the .csproj files)");
                var p = Read(file, root);
                var refs = p.References.Select(r => (MetadataReference)MetadataReference.CreateFromFile(r)).ToList();
                foreach (var dep in p.ProjectReferences)
                {
                    if (byName.TryGetValue(dep, out var d)) refs.Add(d.Compilation.ToMetadataReference());
                    else
                    {
                        string dll = Path.Combine(root, "Library", "ScriptAssemblies", dep + ".dll");
                        if (File.Exists(dll)) refs.Add(MetadataReference.CreateFromFile(dll));
                    }
                }
                p.Compilation = Compile(p, refs);
                byName[name] = p;
                list.Add(p);
            }

            // The exercise stubs (Assets/EvoSim/Exercises/*.cs.txt) are student files to be: compiled against the runtime,
            // as EvoSim > Exercises writes them into Assets/Student.
            var runtime = byName["EvoSim.Runtime"];
            string stubs = Path.Combine(root, "Assets", "EvoSim", "Exercises");
            if (Directory.Exists(stubs))
            {
                var ex = new Project { Name = "EvoSim.Exercises", Defines = runtime.Defines, Language = runtime.Language };
                ex.Sources.AddRange(Directory.GetFiles(stubs, "*.cs.txt").OrderBy(f => f, StringComparer.Ordinal));
                var refs = runtime.References.Select(r => (MetadataReference)MetadataReference.CreateFromFile(r)).ToList();
                refs.Add(runtime.Compilation.ToMetadataReference());
                ex.Compilation = Compile(ex, refs);
                list.Add(ex);
            }
            return list;
        }

        /// <summary>Reads one Unity project file: sources, references, defines and language version.</summary>
        static Project Read(string csproj, string root)
        {
            var doc = XDocument.Load(csproj);
            var ns = doc.Root.Name.Namespace;
            string Full(string path) => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
            var p = new Project { Name = Path.GetFileNameWithoutExtension(csproj) };
            p.Sources.AddRange(doc.Descendants(ns + "Compile").Select(e => (string)e.Attribute("Include")).Where(s => s != null).Select(Full));
            p.References.AddRange(doc.Descendants(ns + "HintPath").Select(e => Full(e.Value.Trim())).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase));
            p.ProjectReferences.AddRange(doc.Descendants(ns + "ProjectReference").Select(e => Path.GetFileNameWithoutExtension((string)e.Attribute("Include"))));
            p.Defines = (doc.Descendants(ns + "DefineConstants").FirstOrDefault()?.Value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            string lang = doc.Descendants(ns + "LangVersion").FirstOrDefault()?.Value;
            if (lang != null && LanguageVersionFacts.TryParse(lang, out var v)) p.Language = v;
            return p;
        }

        static CSharpParseOptions ParseOptions(Project p, DocumentationMode mode) =>
            new CSharpParseOptions(p.Language, mode, SourceCodeKind.Regular, p.Defines);

        static CSharpCompilation Compile(Project p, List<MetadataReference> refs)
        {
            var options = ParseOptions(p, DocumentationMode.Diagnose);
            var trees = p.Sources.Select(f => CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(f), Encoding.UTF8), options, f));
            var compile = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, warningLevel: 4,
                nullableContextOptions: NullableContextOptions.Disable);
            return CSharpCompilation.Create(p.Name, trees, refs, compile);
        }

        // ---------------------------------------------------------------- audit

        /// <summary>The compiler's documentation warnings and the declarations it does not check (non-public ones).</summary>
        static IEnumerable<Issue> Audit(Project p, string root)
        {
            foreach (var d in p.Compilation.GetDiagnostics())
                if (DocCodes.Contains(d.Id) && d.Location.IsInSource && !(d.Id == "CS1591" && IsDescribedTest(d.Location)))
                    yield return At(d.Location, root, d.Id, Shorten(d.GetMessage()));

            foreach (var tree in p.Compilation.SyntaxTrees)
            {
                var model = p.Compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    if (node is BaseNamespaceDeclarationSyntax || node is GlobalStatementSyntax || node is IncompleteMemberSyntax) continue;
                    var symbol = Declared(node, model);
                    if (symbol == null || symbol.IsImplicitlyDeclared) continue;
                    var where = node is BaseFieldDeclarationSyntax f ? f.Declaration.Variables[0].Identifier.GetLocation() : Identifier(node);
                    bool visible = IsPubliclyVisible(symbol);
                    if (!HasDoc(node))
                    {
                        if (visible || Exempt(node, symbol)) continue;          // the compiler reports public ones (CS1591)
                        yield return At(where, root, "DOC01", $"no doc comment: {Kind(symbol)} {Name(symbol)}");
                        continue;
                    }
                    if (!visible || InheritsDoc(node)) continue;
                    foreach (var x in Completeness(symbol)) yield return At(where, root, "DOC02", $"{Name(symbol)}: {x}");
                }
            }
        }

        static ISymbol Declared(MemberDeclarationSyntax node, SemanticModel model) => node switch
        {
            BaseFieldDeclarationSyntax f => model.GetDeclaredSymbol(f.Declaration.Variables[0]),
            _ => model.GetDeclaredSymbol(node),
        };

        static Location Identifier(MemberDeclarationSyntax node) => node switch
        {
            BaseTypeDeclarationSyntax t => t.Identifier.GetLocation(),
            DelegateDeclarationSyntax d => d.Identifier.GetLocation(),
            MethodDeclarationSyntax m => m.Identifier.GetLocation(),
            ConstructorDeclarationSyntax c => c.Identifier.GetLocation(),
            PropertyDeclarationSyntax pr => pr.Identifier.GetLocation(),
            EventDeclarationSyntax e => e.Identifier.GetLocation(),
            EnumMemberDeclarationSyntax em => em.Identifier.GetLocation(),
            _ => node.GetLocation(),
        };

        /// <summary>Public, or protected in a type that can be derived from, all the way up: what the API site shows.</summary>
        static bool IsPubliclyVisible(ISymbol s)
        {
            for (var x = s; x != null && !(x is INamespaceSymbol); x = x.ContainingSymbol)
            {
                switch (x.DeclaredAccessibility)
                {
                    case Accessibility.Public: break;
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        if (x.ContainingType != null && x.ContainingType.IsSealed) return false;
                        break;
                    default: return false;
                }
            }
            return true;
        }

        static bool HasDoc(SyntaxNode node) => node.GetLeadingTrivia().Any(t =>
            t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        static DocumentationCommentTriviaSyntax DocOf(SyntaxNode node) =>
            node.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();

        static bool InheritsDoc(SyntaxNode node)
        {
            var doc = DocOf(node);
            return doc != null && doc.DescendantNodes().Any(n =>
                n is XmlEmptyElementSyntax e && e.Name.LocalName.Text == "inheritdoc" ||
                n is XmlElementSyntax x && x.StartTag.Name.LocalName.Text == "inheritdoc");
        }

        /// <summary>
        /// Declarations documented another way: a field with a Tooltip (the Inspector shows it) or a // comment, a field that
        /// only backs a property, a test with a Description, or a part of a partial type documented elsewhere.
        /// </summary>
        static bool Exempt(MemberDeclarationSyntax node, ISymbol symbol)
        {
            if (node is BaseFieldDeclarationSyntax field)
            {
                if (HasAttribute(node, "Tooltip")) return true;
                if (field.SemicolonToken.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))) return true;
                if (CommentRightAbove(node)) return true;
                if (symbol is IFieldSymbol fs && fs.DeclaredAccessibility == Accessibility.Private)
                {
                    string name = fs.Name.StartsWith("m_") ? fs.Name.Substring(2) : fs.Name.TrimStart('_');
                    if (name.Length > 0)
                    {
                        string property = char.ToUpperInvariant(name[0]) + name.Substring(1);
                        if (fs.ContainingType.GetMembers(property).OfType<IPropertySymbol>().Any()) return true;
                    }
                }
            }
            if (node is MethodDeclarationSyntax method && HasDescription(method)) return true;
            if (symbol is INamedTypeSymbol type && type.DeclaringSyntaxReferences.Length > 1 &&
                type.DeclaringSyntaxReferences.Any(r => HasDoc(r.GetSyntax())))
                return true;
            return false;
        }

        /// <summary>A test whose [Test, Description("…")] (or [Description]) says what it checks: that is its documentation.</summary>
        static bool HasDescription(MethodDeclarationSyntax method) => method.AttributeLists.SelectMany(l => l.Attributes).Any(a =>
            Short(a.Name.ToString()) == "Description" ||
            a.ArgumentList != null && a.ArgumentList.Arguments.Any(x => x.NameEquals?.Name.Identifier.Text == "Description"));

        /// <summary>True when the compiler's "no doc comment" points at a public test method that has a Description.</summary>
        static bool IsDescribedTest(Location location)
        {
            var node = location.SourceTree.GetRoot().FindNode(location.SourceSpan);
            var method = node as MethodDeclarationSyntax ?? node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            return method != null && method.Identifier.Span.Contains(location.SourceSpan) && HasDescription(method);
        }

        static bool HasAttribute(MemberDeclarationSyntax node, string name) =>
            node.AttributeLists.SelectMany(l => l.Attributes).Any(a => Short(a.Name.ToString()) == name);

        static string Short(string attribute)
        {
            string s = attribute.Substring(attribute.LastIndexOf('.') + 1);
            return s.EndsWith("Attribute") ? s.Substring(0, s.Length - "Attribute".Length) : s;
        }

        /// <summary>A // comment on the line right above the declaration (blank lines break it).</summary>
        static bool CommentRightAbove(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia();
            for (int i = trivia.Count - 1; i >= 0; i--)
            {
                var t = trivia[i];
                if (t.IsKind(SyntaxKind.WhitespaceTrivia)) continue;
                if (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)) return true;
                if (t.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    if (i > 0 && trivia[i - 1].IsKind(SyntaxKind.EndOfLineTrivia)) return false;
                    if (i > 1 && trivia[i - 1].IsKind(SyntaxKind.WhitespaceTrivia) && trivia[i - 2].IsKind(SyntaxKind.EndOfLineTrivia)) return false;
                    continue;
                }
                return false;
            }
            return false;
        }

        /// <summary>For a public member with a doc comment: a param for every parameter, a typeparam for every type parameter, a returns for a value.</summary>
        static IEnumerable<string> Completeness(ISymbol symbol)
        {
            XElement doc;
            try { doc = XElement.Parse("<doc>" + StripMember(symbol.GetDocumentationCommentXml() ?? "") + "</doc>"); }
            catch { yield break; }                                                      // CS1570 reports bad XML
            var names = new HashSet<string>(doc.Elements("param").Select(e => (string)e.Attribute("name")));
            var typeNames = new HashSet<string>(doc.Elements("typeparam").Select(e => (string)e.Attribute("name")));
            IEnumerable<IParameterSymbol> parameters = symbol switch
            {
                IMethodSymbol m => m.Parameters,
                IPropertySymbol pr => pr.Parameters,
                INamedTypeSymbol t when t.DelegateInvokeMethod != null => t.DelegateInvokeMethod.Parameters,
                _ => Enumerable.Empty<IParameterSymbol>(),
            };
            foreach (var p in parameters)
                if (!names.Contains(p.Name)) yield return $"no <param name=\"{p.Name}\">";
            IEnumerable<ITypeParameterSymbol> typeParameters = symbol switch
            {
                IMethodSymbol m => m.TypeParameters,
                INamedTypeSymbol t => t.TypeParameters,
                _ => Enumerable.Empty<ITypeParameterSymbol>(),
            };
            foreach (var t in typeParameters)
                if (!typeNames.Contains(t.Name)) yield return $"no <typeparam name=\"{t.Name}\">";
            var method = symbol as IMethodSymbol ?? (symbol as INamedTypeSymbol)?.DelegateInvokeMethod;
            bool returns = method != null && !method.ReturnsVoid &&
                           (method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.DelegateInvoke ||
                            method.MethodKind == MethodKind.UserDefinedOperator || method.MethodKind == MethodKind.Conversion);
            if (returns && doc.Element("returns") == null) yield return "no <returns>";
        }

        static string StripMember(string xml)
        {
            int open = xml.IndexOf("<member", StringComparison.Ordinal);
            if (open < 0) return xml;
            int start = xml.IndexOf('>', open) + 1;
            int end = xml.LastIndexOf("</member>", StringComparison.Ordinal);
            return end > start ? xml.Substring(start, end - start) : xml;
        }

        static string Kind(ISymbol s) => s switch
        {
            INamedTypeSymbol t => t.TypeKind.ToString().ToLowerInvariant(),
            IMethodSymbol m => m.MethodKind == MethodKind.Constructor ? "constructor" : "method",
            IPropertySymbol p => p.IsIndexer ? "indexer" : "property",
            IFieldSymbol f => f.ContainingType.TypeKind == TypeKind.Enum ? "enum member" : f.IsConst ? "constant" : "field",
            IEventSymbol _ => "event",
            _ => s.Kind.ToString().ToLowerInvariant(),
        };

        static string Name(ISymbol s) => s is INamedTypeSymbol ? s.ToDisplayString() : $"{s.ContainingType?.Name}.{s.Name}";

        static string Shorten(string message) =>
            message.Replace("Missing XML comment for publicly visible type or member ", "no doc comment: ");

        static Issue At(Location location, string root, string code, string text)
        {
            var span = location.GetLineSpan();
            return new Issue { File = Relative(span.Path, root), Line = span.StartLinePosition.Line + 1, Code = code, Text = text };
        }

        static string Relative(string path, string root)
        {
            string rel = Path.GetRelativePath(root, path).Replace('\\', '/');
            return rel.StartsWith(CodeRoot, StringComparison.Ordinal) ? rel.Substring(CodeRoot.Length) : rel;
        }

        static bool InScope(string file, List<string> only) =>
            only.Count == 0 || only.Any(o => file.StartsWith(o + "/", StringComparison.Ordinal) || file == o);

        static void Report(List<Issue> issues, List<string> only, int max)
        {
            Console.WriteLine($"EvoSim doc audit{(only.Count > 0 ? " — " + string.Join(", ", only) : "")}: {issues.Count} issue(s)");
            foreach (var g in issues.GroupBy(x => x.Code).OrderBy(g => g.Key))
                Console.WriteLine($"  {g.Key} {Describe(g.Key),-34} {g.Count(),5}");
            if (issues.Count == 0) return;
            Console.WriteLine("  by folder:");
            foreach (var g in issues.GroupBy(x => Path.GetDirectoryName(x.File)?.Replace('\\', '/') ?? "").OrderBy(g => g.Key, StringComparer.Ordinal))
                Console.WriteLine($"    {g.Key,-40} {g.Count(),5}");
            foreach (var x in issues.Take(max)) Console.WriteLine("  " + x);
            if (issues.Count > max) Console.WriteLine($"  … {issues.Count - max} more (--max N)");
        }

        static string Describe(string code) => code switch
        {
            "CS1591" => "public, no doc comment",
            "DOC01" => "non-public, no doc comment",
            "DOC02" => "param / typeparam / returns missing",
            "CS1570" => "badly formed XML",
            "CS1572" => "param for a missing parameter",
            "CS1573" => "parameter without param",
            "CS1574" => "cref not found",
            "CS1580" or "CS1581" or "CS1584" or "CS1658" => "bad cref",
            "CS1587" => "doc comment on nothing",
            _ => "",
        };

        // ---------------------------------------------------------------- outputs

        /// <summary>Writes each assembly with its XML documentation file (the input of DocFX's DLL mode).</summary>
        static void WriteXml(List<Project> projects, string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (var p in projects.Where(p => p.Name != "EvoSim.Exercises"))
            {
                using var dll = File.Create(Path.Combine(folder, p.Name + ".dll"));
                using var xml = File.Create(Path.Combine(folder, p.Name + ".xml"));
                var result = p.Compilation.Emit(dll, xmlDocumentationStream: xml);
                if (!result.Success) Console.WriteLine($"{p.Name}: emit failed");
            }
            Console.WriteLine($"assemblies and XML documentation written to {folder}");
        }

        /// <summary>
        /// The extension-points page: every public type a student can derive from or plug into, with its abstract and
        /// virtual members, its events, and the EvoSim types that already implement it.
        /// </summary>
        static void WriteInventory(List<Project> projects, string file)
        {
            var api = new[] { "EvoSim.Runtime", "EvoSim.Http", "EvoSim.Testing", "EvoSim.Editor" };
            var all = projects.Where(p => p.Name != "EvoSim.Exercises")
                              .SelectMany(p => Types(p.Compilation.Assembly.GlobalNamespace).Select(t => (p.Name, t))).ToList();
            var derived = new Dictionary<string, List<INamedTypeSymbol>>();
            foreach (var (_, t) in all)
            {
                if (t.DeclaredAccessibility != Accessibility.Public || t.ContainingNamespace.ToDisplayString().StartsWith("EvoSim.Tests")) continue;
                var bases = new List<INamedTypeSymbol>();
                for (var b = t.BaseType; b != null; b = b.BaseType) bases.Add(b.OriginalDefinition);
                bases.AddRange(t.AllInterfaces.Select(i => i.OriginalDefinition));
                foreach (var b in bases)
                {
                    string key = b.GetDocumentationCommentId();
                    if (key == null) continue;
                    if (!derived.TryGetValue(key, out var l)) derived[key] = l = new List<INamedTypeSymbol>();
                    if (!l.Any(x => x.GetDocumentationCommentId() == t.GetDocumentationCommentId())) l.Add(t);
                }
            }

            var sb = new StringBuilder();
            sb.Append("---\nuid: extension-points\n---\n\n");
            sb.Append("# Every extension point\n\n");
            sb.Append("> Generated by `Docs/Site/tools/DocAudit --inventory` from the code: do not edit by hand.\n\n");
            sb.Append("Every public type you can derive from or plug into, with the members you implement (**abstract**) or may\n");
            sb.Append("replace (**virtual**), its events, and the EvoSim classes that already do it: read those first.\n");
            sb.Append("The [extension guide](index.md) explains when to use which.\n\n");
            foreach (var group in all.Where(x => api.Contains(x.Name)).GroupBy(x => x.Name))
            {
                var entries = group.Select(x => x.t).Where(IsExtensionPoint).OrderBy(t => t.ContainingNamespace.ToDisplayString()).ThenBy(t => t.Name).ToList();
                if (entries.Count == 0) continue;
                sb.Append($"## {group.Key}\n\n");
                foreach (var t in entries)
                {
                    string kind = t.TypeKind == TypeKind.Interface ? "interface" : t.IsAbstract ? "abstract class"
                                : IsAttribute(t) ? "attribute" : "class";
                    sb.Append($"### {Escape(DisplayName(t))}\n\n");
                    sb.Append($"<xref:{Xref(t)}> — {kind}. {Escape(FirstSentence(t))}\n\n");
                    var members = ExtensionMembers(t).ToList();
                    if (members.Count > 0)
                    {
                        sb.Append("| Member | | What it does |\n|---|---|---|\n");
                        foreach (var m in members)
                            sb.Append($"| [{Escape(MemberLabel(m))}](<xref:{Xref(m)}>) | {MemberKind(m)} | {Escape(FirstSentence(m))} |\n");
                        sb.Append('\n');
                    }
                    if (derived.TryGetValue(t.GetDocumentationCommentId(), out var impl) && impl.Count > 0)
                        sb.Append("In EvoSim: " + string.Join(", ", impl.OrderBy(x => x.Name).Select(x => $"<xref:{Xref(x)}>")) + ".\n\n");
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, sb.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
            Console.WriteLine($"extension points written to {file}");
        }

        static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns)
        {
            foreach (var t in ns.GetTypeMembers())
            {
                yield return t;
                foreach (var n in Nested(t)) yield return n;
            }
            foreach (var child in ns.GetNamespaceMembers())
                foreach (var t in Types(child)) yield return t;
        }

        static IEnumerable<INamedTypeSymbol> Nested(INamedTypeSymbol t)
        {
            foreach (var n in t.GetTypeMembers())
            {
                yield return n;
                foreach (var m in Nested(n)) yield return m;
            }
        }

        static bool IsAttribute(INamedTypeSymbol t)
        {
            for (var b = t.BaseType; b != null; b = b.BaseType)
                if (b.ToDisplayString() == "System.Attribute") return true;
            return false;
        }

        static bool IsExtensionPoint(INamedTypeSymbol t) =>
            IsPubliclyVisible(t) && t.TypeKind != TypeKind.Enum && t.TypeKind != TypeKind.Delegate &&
            (t.TypeKind == TypeKind.Interface || t.IsAbstract && !t.IsStatic || IsAttribute(t) || ExtensionMembers(t).Any());

        /// <summary>Abstract and virtual members a subclass may implement, events it may hear, interface members.</summary>
        static IEnumerable<ISymbol> ExtensionMembers(INamedTypeSymbol t)
        {
            foreach (var m in t.GetMembers().Where(m => !m.IsImplicitlyDeclared && IsPubliclyVisible(m)).OrderBy(Order).ThenBy(m => m.Name))
            {
                if (m is IMethodSymbol method && method.MethodKind != MethodKind.Ordinary) continue;
                if (m is INamedTypeSymbol) continue;
                if (t.TypeKind == TypeKind.Interface || m is IEventSymbol) { yield return m; continue; }
                if (t.IsSealed) continue;
                if (m.IsAbstract || m.IsVirtual) yield return m;
            }
        }

        static int Order(ISymbol m) => m.IsAbstract ? 0 : m.IsVirtual ? 1 : 2;

        static string MemberKind(ISymbol m)
        {
            string what = m is IEventSymbol ? "event" : m is IPropertySymbol ? "property" : "method";
            if (m.ContainingType.TypeKind == TypeKind.Interface) return what;
            if (m is IEventSymbol) return m.IsStatic ? "static event" : "event";
            return (m.IsAbstract ? "abstract " : "virtual ") + what;
        }

        static string MemberLabel(ISymbol m) =>
            m is IMethodSymbol method ? $"{m.Name}({string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))})" : m.Name;

        static string DisplayName(INamedTypeSymbol t) => t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        /// <summary>The DocFX uid of a symbol: its documentation comment id without the "T:" or "M:" prefix.</summary>
        static string Uid(ISymbol s)
        {
            string id = s.OriginalDefinition.GetDocumentationCommentId() ?? s.ToDisplayString();
            return id.Length > 2 && id[1] == ':' ? id.Substring(2) : id;
        }

        static string FirstSentence(ISymbol s)
        {
            string xml = s.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(xml)) return "";
            try
            {
                var doc = XElement.Parse("<doc>" + StripMember(xml) + "</doc>");
                var summary = doc.Element("summary");
                if (summary == null) return doc.Element("inheritdoc") != null ? "(as the base)" : "";
                string text = string.Concat(summary.Nodes().Select(Flatten)).Trim();
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
                int end = SentenceEnd(text);
                return end > 0 ? text.Substring(0, end) : text;
            }
            catch { return ""; }
        }

        static int SentenceEnd(string text)
        {
            for (int i = 0; i < text.Length - 1; i++)
                if ((text[i] == '.' || text[i] == ':') && text[i + 1] == ' ' && i > 2 && !text.Substring(0, i).EndsWith("e.g") && !text.Substring(0, i).EndsWith("i.e"))
                    return text[i] == '.' ? i + 1 : -1;
            return -1;
        }

        static string Flatten(XNode n) => n switch
        {
            XText t => t.Value,
            XElement e when e.Name == "see" || e.Name == "seealso" =>
                e.Attribute("langword")?.Value ?? (string)e ?? ShortCref((string)e.Attribute("cref") ?? ""),
            XElement e when e.Name == "paramref" || e.Name == "typeparamref" => (string)e.Attribute("name"),
            XElement e => string.Concat(e.Nodes().Select(Flatten)),
            _ => "",
        };

        static string ShortCref(string cref)
        {
            string s = cref.Length > 2 && cref[1] == ':' ? cref.Substring(2) : cref;
            int paren = s.IndexOf('(');
            if (paren >= 0) s = s.Substring(0, paren);
            s = s.Substring(s.LastIndexOf('.') + 1);
            return s.Replace("`1", "<T>").Replace("`2", "<T1,T2>");
        }

        static string Escape(string s) => s.Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>A uid as a Markdown xref link wants it: the backtick of generic types URL-encoded.</summary>
        static string Xref(ISymbol s) => Uid(s).Replace("`", "%60");

        // ---------------------------------------------------------------- API table of contents

        /// <summary>The API groups of the site, by source folder (relative to Assets/EvoSim), in reading order.</summary>
        static readonly (string Folder, string Title)[] ApiGroups =
        {
            ("Runtime/Core", "Core: world, species, animals, ticks"),
            ("Runtime/Phases", "Tick phases"),
            ("Runtime/Senses", "Senses"),
            ("Runtime/Brains", "Decisions and brains"),
            ("Http", "HTTP brains and the mutator client"),
            ("Runtime/Actions", "Actions and interactions"),
            ("Runtime/Locomotion", "Locomotion"),
            ("Runtime/Food", "Food web"),
            ("Runtime/Stats", "Stats, metabolism and death"),
            ("Runtime/Genes", "Genes and alleles"),
            ("Runtime/Mutation", "Mutation"),
            ("Runtime/Reproduction", "Reproduction"),
            ("Runtime/Population", "Population"),
            ("Runtime/Environment", "Environment: food, cover, entities"),
            ("Runtime/Ground", "Ground"),
            ("Runtime/Recording", "Recording"),
            ("Runtime/Config", "Scenarios and configuration"),
            ("Runtime/Compatibility", "Prototype formats"),
            ("Runtime/Views", "Views"),
            ("Runtime/Player", "Player builds"),
            ("Samples", "Samples"),
            ("Testing", "Testing"),
            ("Testing/Probes", "Testing: probes"),
            ("Editor", "Editor"),
        };

        /// <summary>
        /// Replaces DocFX's flat API table of contents (one namespace holds most types) with one grouped by source folder,
        /// and writes the API home page: every type of each group with the first sentence of its summary. The types are the
        /// ones DocFX wrote a page for (its *.yml files), so the two always agree.
        /// </summary>
        static void WriteApiToc(List<Project> projects, string apiDir, string root)
        {
            var byFile = new Dictionary<string, (INamedTypeSymbol Type, string Folder)>(StringComparer.Ordinal);
            var namespaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in projects.Where(p => p.Name != "EvoSim.Exercises" && !p.Name.StartsWith("EvoSim.Tests")))
                foreach (var t in Types(p.Compilation.Assembly.GlobalNamespace))
                {
                    if (!t.ContainingNamespace.IsGlobalNamespace) namespaces.Add(t.ContainingNamespace.ToDisplayString());
                    var location = t.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location == null) continue;
                    string folder = Path.GetDirectoryName(Relative(location.SourceTree.FilePath, root))?.Replace('\\', '/') ?? "";
                    byFile[Uid(t).Replace('`', '-')] = (t, folder);
                }

            var groups = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
            var other = new List<string>();
            foreach (var file in Directory.GetFiles(apiDir, "*.yml").Select(Path.GetFileNameWithoutExtension).OrderBy(f => f, StringComparer.Ordinal))
            {
                if (file == "toc" || namespaces.Contains(file)) continue;
                if (!byFile.TryGetValue(file, out var entry)) { other.Add(file); continue; }
                if (!groups.TryGetValue(entry.Folder, out var list)) groups[entry.Folder] = list = new List<INamedTypeSymbol>();
                list.Add(entry.Type);
            }

            var toc = new StringBuilder("### YamlMime:TableOfContent\nitems:\n");
            var index = new StringBuilder();
            index.Append("---\nuid: api-index\n---\n\n# API reference\n\n");
            index.Append("> Generated by `Docs/Site/tools/DocAudit --api-toc` from the code and its documentation comments.\n\n");
            index.Append("Every public type of EvoSim, grouped by the folder of `Assets/EvoSim` it lives in. Each page lists the type's\n");
            index.Append("members with their documentation, and links to its source on GitHub. Where to start: <xref:EvoSim.World>,\n");
            index.Append("<xref:EvoSim.Species>, <xref:EvoSim.SpeciesModule>, and the [extension points](../extending/extension-points.md).\n\n");
            var order = ApiGroups.Select(g => g.Folder).ToList();
            foreach (var folder in groups.Keys.OrderBy(f => order.IndexOf(f) < 0 ? int.MaxValue : order.IndexOf(f)).ThenBy(f => f, StringComparer.Ordinal))
            {
                string title = ApiGroups.FirstOrDefault(g => g.Folder == folder).Title ?? folder;
                var types = groups[folder].OrderBy(TypeLabel, StringComparer.Ordinal).ToList();
                toc.Append($"- name: \"{title}\"\n  items:\n");
                foreach (var t in types) toc.Append($"  - uid: \"{Uid(t)}\"\n    name: \"{TypeLabel(t)}\"\n");
                index.Append($"## {title}\n\n`Assets/EvoSim/{folder}`\n\n| Type | | Summary |\n|---|---|---|\n");
                foreach (var t in types) index.Append($"| <xref:{Xref(t)}> | {TypeKindLabel(t)} | {Escape(FirstSentence(t))} |\n");
                index.Append('\n');
            }
            foreach (var f in other) Console.WriteLine($"  api page without a known type (left out of the table of contents): {f}");
            File.WriteAllText(Path.Combine(apiDir, "toc.yml"), toc.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(apiDir, "index.md"), index.ToString(), new UTF8Encoding(false));
            Console.WriteLine($"API table of contents: {groups.Values.Sum(l => l.Count)} types in {groups.Count} groups ({apiDir})");
        }

        static string TypeLabel(INamedTypeSymbol t) =>
            (t.ContainingType != null ? TypeLabel(t.ContainingType) + "." : "") + t.Name +
            (t.TypeParameters.Length > 0 ? "<" + string.Join(", ", t.TypeParameters.Select(p => p.Name)) + ">" : "");

        static string TypeKindLabel(INamedTypeSymbol t) => t.TypeKind switch
        {
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            TypeKind.Struct => "struct",
            TypeKind.Delegate => "delegate",
            _ => t.IsStatic ? "static class" : t.IsAbstract ? "abstract class" : IsAttribute(t) ? "attribute" : "class",
        };

        // ---------------------------------------------------------------- specification

        /// <summary>The specification documents the site publishes (Docs/Interface), with the table-of-contents parts.</summary>
        static readonly (string Part, string[] Files)[] SpecParts =
        {
            ("", new[] { "README.md" }),
            ("Part A — the contract", new[]
            {
                "01-concepts-and-glossary.md", "02-space-and-time.md", "03-environment.md", "04-species-and-food-web.md",
                "05-animals-stats-and-life-cycle.md", "06-senses-and-observations.md", "07-actions-and-locomotion.md",
                "08-decisions-brains-and-prompts.md", "09-genes-alleles-and-genomes.md", "10-mutation.md",
                "11-reproduction-and-population.md", "12-tick-order-and-determinism.md", "13-outputs-and-recording.md",
                "14-configuration-reference.md", "15-controls-metrics-and-gates.md",
            }),
            ("Part B — the Unity implementation", new[] { "20-unity-architecture.md", "21-editor-tooling.md", "22-extending-recipes.md" }),
            ("Part C — verification", new[] { "30-tests.md", "31-scenarios.md", "32-integrity-prompts-and-ci.md", "40-open-questions.md" }),
        };

        /// <summary>Notes the site adds under the title of a specification document (the source files stay as they are).</summary>
        static readonly Dictionary<string, string> SpecNotes = new Dictionary<string, string>
        {
            ["20-unity-architecture.md"] =
                "> [!NOTE]\n> This document is the design proposal written before the code. Its code sketches show the ideas, but " +
                "some names and signatures differ from the code as it is now. For the real classes, see the " +
                "[guides](../guides/index.md) and the [API reference](../api/index.md).",
            ["22-extending-recipes.md"] =
                "> [!NOTE]\n> These recipes were written before the code. Their sketches show the ideas, but some names and " +
                "signatures differ from the code as it is now. Code that compiles is in the samples (`Assets/EvoSim/Samples`, " +
                "[The samples](../extending/samples.md)) and on the [Extending](../extending/index.md) pages.",
        };

        /// <summary>
        /// Copies the specification (Docs/Interface) into the site's build folder with a table of contents; links to files
        /// the site does not publish (prompts, the implementation log, Assets…) become links to the file on GitHub.
        /// </summary>
        static void WriteSpec(string root, string dest)
        {
            string source = Path.Combine(root, "Docs", "Interface");
            string web = GitHubBase(root);
            var published = new HashSet<string>(SpecParts.SelectMany(p => p.Files), StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.CreateDirectory(dest);
            var link = new System.Text.RegularExpressions.Regex(@"\]\((?<target>[^)\s#]+)(?<anchor>#[^)\s]*)?\)");
            var toc = new StringBuilder();
            foreach (var (part, files) in SpecParts)
            {
                if (part != "") toc.Append($"- name: \"{part}\"\n  items:\n");
                foreach (var file in files)
                {
                    string text = File.ReadAllText(Path.Combine(source, file));
                    text = link.Replace(text, m =>
                    {
                        string target = m.Groups["target"].Value;
                        if (target.Contains("://") || target.StartsWith("mailto:")) return m.Value;
                        if (published.Contains(target)) return m.Value;
                        string full = Path.GetFullPath(Path.Combine(source, target.Replace('/', Path.DirectorySeparatorChar)));
                        string rel = Path.GetRelativePath(root, full).Replace('\\', '/');
                        if (rel.StartsWith("..")) return m.Value;
                        string kind = Directory.Exists(full) ? "tree" : "blob";
                        return $"]({web}/{kind}/{GitBranch(root)}/{rel}{m.Groups["anchor"].Value})";
                    });
                    text = text.Replace("\r\n", "\n");
                    if (SpecNotes.TryGetValue(file, out var note))
                    {
                        int heading = text.IndexOf("# ", StringComparison.Ordinal);
                        int after = heading < 0 ? 0 : text.IndexOf('\n', heading) + 1;
                        text = text.Insert(after, "\n" + note + "\n");
                    }
                    File.WriteAllText(Path.Combine(dest, file), text, new UTF8Encoding(false));
                    string title = text.Split('\n').FirstOrDefault(l => l.StartsWith("# "))?.Substring(2).Trim() ?? file;
                    if (file == "README.md") title = "Overview";
                    toc.Append(part != "" ? $"  - name: \"{title.Replace("\"", "'")}\"\n    href: {file}\n" : $"- name: \"{title}\"\n  href: {file}\n");
                }
            }
            File.WriteAllText(Path.Combine(dest, "toc.yml"), toc.ToString(), new UTF8Encoding(false));
            Console.WriteLine($"specification copied to {dest}");
        }

        static string GitHubBase(string root)
        {
            string url = Git(root, "remote get-url origin").Trim();
            if (url.EndsWith(".git")) url = url.Substring(0, url.Length - 4);
            if (url.StartsWith("git@github.com:")) url = "https://github.com/" + url.Substring("git@github.com:".Length);
            return url;
        }

        static string gitBranch;
        static string GitBranch(string root) => gitBranch ??= Git(root, "rev-parse --abbrev-ref HEAD").Trim();

        static string Git(string root, string arguments)
        {
            var info = new ProcessStartInfo("git", arguments) { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false };
            using var git = Process.Start(info);
            string output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return output;
        }

        // ---------------------------------------------------------------- examples

        /// <summary>
        /// The usings an example gets, as a student's file in Assets/Student would have them: NUnit only for a test (its
        /// Range would clash with Unity's), UnityEditor and EvoSim.Editor only for editor code.
        /// </summary>
        static string ExampleUsings(string code)
        {
            string usings = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Threading.Tasks;\n" +
                            "using UnityEngine;\nusing EvoSim;\nusing EvoSim.Testing;\nusing EvoSim.Samples;\n";
            if (code.Contains("[Test") || code.Contains("[UnityTest") || code.Contains("Assert.")) usings += "using NUnit.Framework;\n";
            if (System.Text.RegularExpressions.Regex.IsMatch(code, @"\b(MenuItem|EditorWindow|CustomEditor|EditorGUILayout|EditorGUI|Selection|AssetDatabase|SerializedObject)\b"))
                usings += "using UnityEditor;\nusing EvoSim.Editor;\n";
            return usings;
        }

        /// <summary>
        /// Compiles every ```csharp block of the site's pages that declares a whole type, against the EvoSim assemblies
        /// (an example a student copies must compile). Shorter fragments (a method, a few statements) are counted, not
        /// compiled. Each block is compiled on its own, so two examples may declare the same class.
        /// </summary>
        static int CheckExamples(List<Project> projects, string folder, string root)
        {
            var refs = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in projects.Where(p => p.Name == "EvoSim.Testing" || p.Name == "EvoSim.Editor"))
                foreach (var r in p.References) refs[Path.GetFileName(r)] = MetadataReference.CreateFromFile(r);
            var all = refs.Values.ToList();
            all.AddRange(projects.Where(p => !p.Name.StartsWith("EvoSim.Tests") && p.Name != "EvoSim.Exercises")
                                 .Select(p => p.Compilation.ToMetadataReference()));
            var runtime = projects.First(p => p.Name == "EvoSim.Runtime");
            var parse = ParseOptions(runtime, DocumentationMode.None);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Disable);
            var typeDeclaration = new System.Text.RegularExpressions.Regex(@"^(\[[^\]]*\]\s*)*((public|internal|sealed|abstract|static|partial)\s+)*(class|struct|interface|enum)\s+\w+",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            List<Diagnostic> Errors(string code, IEnumerable<string> before)
            {
                var trees = before.Select(b => CSharpSyntaxTree.ParseText(ExampleUsings(b) + b, parse)).ToList();
                var tree = CSharpSyntaxTree.ParseText(ExampleUsings(code) + code, parse);
                trees.Add(tree);
                var compilation = CSharpCompilation.Create("Example", trees, all, options);
                return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree == tree).ToList();
            }
            static bool OnlyMissingNames(List<Diagnostic> errors) => errors.All(e => e.Id == "CS0246" || e.Id == "CS0103");

            int compiled = 0, fragments = 0;
            var good = new List<string>();                                     // every example that compiled
            var failed = new List<(string File, int Start, string Code, List<Diagnostic> Errors)>();
            var files = folder.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(f => Path.GetFullPath(f.Trim()))
                              .SelectMany(f => File.Exists(f) ? new[] { f } : Directory.GetFiles(f, "*.md", SearchOption.AllDirectories));
            foreach (var file in files.OrderBy(f => f, StringComparer.Ordinal))
            {
                var lines = File.ReadAllLines(file);
                var earlier = new List<string>();                     // the page's examples that compiled, in page order
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim() != "```csharp") continue;
                    int start = i + 1, end = start;
                    while (end < lines.Length && lines[end].Trim() != "```") end++;
                    string code = string.Join("\n", lines.Skip(start).Take(end - start));
                    i = end;
                    bool unit = typeDeclaration.IsMatch(code) || code.Contains("namespace ");
                    if (!unit) { fragments++; continue; }
                    compiled++;
                    var errors = Errors(code, Enumerable.Empty<string>());
                    // A test that uses a class an earlier example of the page declares: compile it with those examples.
                    if (errors.Count > 0 && earlier.Count > 0 && OnlyMissingNames(errors)) errors = Errors(code, earlier);
                    if (errors.Count == 0) { earlier.Add(code); good.Add(code); }
                    else failed.Add((file, start, code, errors));
                }
            }

            // Then one that uses a class another page declares (a test of the-tick.md's example phase, say).
            int broken = 0;
            foreach (var (file, start, code, first) in failed)
            {
                var errors = OnlyMissingNames(first) ? Errors(code, good) : first;
                if (errors.Count == 0) continue;
                broken++;
                int usingLines = ExampleUsings(code).Count(c => c == '\n');
                Console.WriteLine($"  EXAMPLE DOES NOT COMPILE: {Path.GetRelativePath(root, file).Replace('\\', '/')}({start + 1})");
                foreach (var e in errors.Take(4))
                {
                    int line = e.Location.GetLineSpan().StartLinePosition.Line - usingLines;
                    Console.WriteLine($"    block line {line + 1}: {e.Id} {e.GetMessage()}");
                }
            }
            Console.WriteLine($"examples: {compiled} complete one(s) compiled, {broken} with errors; {fragments} fragment(s) not compiled");
            return broken;
        }

        // ---------------------------------------------------------------- same code

        /// <summary>
        /// Compares every EvoSim source file with its version at a git revision, ignoring comments and white space (Roslyn's
        /// syntax equivalence): a documentation pass must leave the code itself, strings included, exactly as it was.
        /// </summary>
        static int CompareCode(string root, string gitRef, List<Project> projects, List<string> only)
        {
            var files = projects.SelectMany(p => p.Sources.Select(s => (p, s))).Where(x => InScope(Relative(x.s, root), only)).ToList();
            var old = GitShow(root, gitRef, files.Select(x => Path.GetRelativePath(root, x.s).Replace('\\', '/')).ToList());
            int changed = 0, same = 0, added = 0;
            foreach (var (p, path) in files)
            {
                string rel = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (!old.TryGetValue(rel, out var before)) { added++; Console.WriteLine($"  new file (not in {gitRef}): {rel}"); continue; }
                string after = File.ReadAllText(path);
                if (after.Contains('\r') != before.Contains('\r') || after.StartsWith('﻿') != before.StartsWith('﻿'))
                {
                    changed++;
                    Console.WriteLine($"  LINE ENDINGS OR BOM CHANGED: {rel}");
                    continue;
                }
                var options = ParseOptions(p, DocumentationMode.None);
                var a = CSharpSyntaxTree.ParseText(before, options);
                var b = CSharpSyntaxTree.ParseText(after, options);
                if (SyntaxFactory.AreEquivalent(a, b, topLevel: false)) same++;
                else { changed++; Console.WriteLine($"  CODE CHANGED: {rel}"); }
            }
            Console.WriteLine($"same code as {gitRef}: {same} file(s); code changed: {changed}; new: {added}");
            return changed;
        }

        /// <summary>File contents at a git revision, read with one "git cat-file --batch".</summary>
        static Dictionary<string, string> GitShow(string root, string gitRef, List<string> paths)
        {
            var result = new Dictionary<string, string>();
            var info = new ProcessStartInfo("git", "cat-file --batch")
            {
                WorkingDirectory = root, RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false,
            };
            using var git = Process.Start(info);
            var output = git.StandardOutput.BaseStream;
            foreach (var path in paths)
            {
                git.StandardInput.Write($"{gitRef}:{path}\n");
                git.StandardInput.Flush();
                string header = ReadLine(output);
                if (header.EndsWith(" missing")) continue;
                int size = int.Parse(header.Substring(header.LastIndexOf(' ') + 1));
                var bytes = new byte[size];
                for (int read = 0; read < size;) read += output.Read(bytes, read, size - read);
                output.ReadByte();                                                      // the newline after the content
                result[path] = Encoding.UTF8.GetString(bytes);
            }
            git.StandardInput.Close();
            git.WaitForExit();
            return result;
        }

        static string ReadLine(Stream s)
        {
            var bytes = new List<byte>();
            for (int b = s.ReadByte(); b >= 0 && b != '\n'; b = s.ReadByte()) bytes.Add((byte)b);
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}
