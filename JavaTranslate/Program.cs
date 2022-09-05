using dnlib.DotNet;
using JavaTranslate.Parsing;
using JavaTranslate.Translation;
using Newtonsoft.Json;

Translator translator = new Translator();
foreach (string path in Directory.EnumerateFiles(args[0], "*.class", SearchOption.AllDirectories)) {
    ClassFile file = new ClassFile(File.ReadAllBytes(path));
    translator.AddClassFile(file);
    File.WriteAllText($"{Path.GetFileNameWithoutExtension(path)}.json", JsonConvert.SerializeObject(file, Formatting.Indented));
}

ModuleDefUser module = translator.Translate();
AssemblyDefUser assembly = new AssemblyDefUser("JavaProgram");
assembly.Modules.Add(module);
module.Write("JavaProgram.dll");
Console.WriteLine("Done!");


