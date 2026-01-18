using System;
using System.IO;

namespace CycloneDDS.CodeGen
{
    public class CodeGenerator
    {
        private readonly SchemaDiscovery _discovery = new SchemaDiscovery();
        // SchemaValidator now requires types, so instantiated in Generate
        private readonly IdlEmitter _idlEmitter = new IdlEmitter();
        private readonly SerializerEmitter _serializerEmitter = new SerializerEmitter();
        private readonly DeserializerEmitter _deserializerEmitter = new DeserializerEmitter();

        public void Generate(string sourceDir, string outputDir)
        {
            Console.WriteLine($"Discovering types in: {sourceDir}");
            var types = _discovery.DiscoverTopics(sourceDir);
            
            Console.WriteLine($"Found {types.Count} type(s)");
            
            // 2. Validate ALL types with strict checking
            var validator = new SchemaValidator(types);
            var managedValidator = new ManagedTypeValidator();
            
            bool hasErrors = false;
            foreach (var type in types)
            {
                var result = validator.Validate(type);
                if (!result.IsValid)
                {
                    hasErrors = true;
                    foreach (var err in result.Errors)
                    {
                        Console.Error.WriteLine($"ERROR: {err}");
                    }
                }

                var managedErrors = managedValidator.Validate(type);
                if (managedErrors.Any(d => d.Severity == ValidationSeverity.Error))
                {
                    hasErrors = true;
                     foreach (var d in managedErrors.Where(d => d.Severity == ValidationSeverity.Error))
                         Console.Error.WriteLine($"ERROR: {d.Message}");
                }
            }
            
            if (hasErrors)
            {
                throw new InvalidOperationException("Schema validation failed. Fix errors above.");
            }
            
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            foreach (var topic in types)
            {
                if (topic.IsTopic || topic.IsStruct)
                {
                    var serializerCode = _serializerEmitter.EmitSerializer(topic);
                    File.WriteAllText(Path.Combine(outputDir, $"{topic.Name}.Serializer.cs"), serializerCode);

                    var deserializerCode = _deserializerEmitter.EmitDeserializer(topic);
                    File.WriteAllText(Path.Combine(outputDir, $"{topic.Name}.Deserializer.cs"), deserializerCode);
                    Console.WriteLine($"    Generated Serializers for {topic.Name}");
                }

                if (topic.IsTopic)
                {
                    Console.WriteLine($"  - {topic.FullName}");
                    
                    var idl = _idlEmitter.EmitIdl(topic);
                    string idlPath = Path.Combine(outputDir, $"{topic.Name}.idl");
                    File.WriteAllText(idlPath, idl);
                    Console.WriteLine($"    Generated {topic.Name}.idl");

                    // --- Descriptor Generation ---
                    try
                    {
                        var idlcRunner = new IdlcRunner();
                        string tempCGroup = Path.Combine(outputDir, "temp_c");
                        if (!Directory.Exists(tempCGroup)) Directory.CreateDirectory(tempCGroup);

                        var result = idlcRunner.RunIdlc(idlPath, tempCGroup);
                        if (result.ExitCode != 0)
                        {
                             Console.Error.WriteLine($"    idlc failed: {result.StandardError}");
                        }
                        else
                        {
                            // Parse C file
                            string cFile = Path.Combine(tempCGroup, $"{topic.Name}.c");
                            if (File.Exists(cFile))
                            {
                                var parser = new DescriptorParser();
                                var metadata = parser.ParseDescriptor(cFile);
                                
                                // Generate Descriptor Code
                                var descCode = GenerateDescriptorCode(topic, metadata);
                                 File.WriteAllText(Path.Combine(outputDir, $"{topic.Name}.Descriptor.cs"), descCode);
                                 Console.WriteLine($"    Generated {topic.Name}.Descriptor.cs");
                            }
                            else
                            {
                                Console.Error.WriteLine($"    Could not find generated C file: {cFile}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                         Console.Error.WriteLine($"    Descriptor generation failed: {ex.Message}");
                         // Don't fail the whole build for now, but warn
                    }
                }
            }
            
            Console.WriteLine($"Output will go to: {outputDir}");
        }

        private string GenerateDescriptorCode(TypeInfo topic, DescriptorMetadata metadata)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using CycloneDDS.Runtime;"); // Assuming generated code usage
            sb.AppendLine();
            sb.AppendLine($"namespace {topic.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial struct {topic.Name}");
            sb.AppendLine("    {");
            
            // Ops
            sb.Append("        private static readonly uint[] _ops = new uint[] {");
            if (metadata.OpsValues != null && metadata.OpsValues.Length > 0)
            {
                 sb.Append(string.Join(", ", metadata.OpsValues));
            }
            sb.AppendLine("};");

            sb.AppendLine();
            sb.AppendLine("        public static uint[] GetDescriptorOps() => _ops;");
            
            // Add IDL string for reference if needed?
            // sb.AppendLine($"        public const string Idl = @\"{topic.Idl}\";");
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString(); 
        }
    }
}
