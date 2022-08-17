// See https://aka.ms/new-console-template for more information
using JavaTranslate.ClassFile;

ClassFile file = new ClassFile(File.ReadAllBytes(args[0]));
Console.WriteLine("Hello, World!");