MobileUO: TODO: currently I am just copying the generated files into the Unity project. In the future, it should be done automatically.

Use this in the TazUO generator to generate the files:

    static void Execute(Compilation compilation, IEnumerable<MethodInfo> methods, SourceProductionContext context)
    {
        if (methods == null || !methods.Any())
            return;

        var outDir = @"C:\Temp\EventGeneratorOutput\";
        var methodsByClass = methods.GroupBy(m => (m.Namespace, m.ClassName));

        foreach (var group in methodsByClass)
        {
            var fileName = $"{group.Key.ClassName}.Events.g.cs";
            var source = GeneratePartialClass(group.Key.Namespace, group.Key.ClassName, group.ToList());
            context.AddSource($"{group.Key.ClassName}.Events.g.cs", SourceText.From(source, Encoding.UTF8));

            if (!string.IsNullOrWhiteSpace(outDir))
            {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
                Directory.CreateDirectory(outDir);
                var path = Path.Combine(outDir, fileName);
                File.WriteAllText(path, source, Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
            }
        }
    }