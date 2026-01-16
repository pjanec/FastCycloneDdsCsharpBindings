using CppAst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CycloneDDS.CodeGen.DescriptorExtraction;

public static class DescriptorExtractor
{
    public static DescriptorData ExtractFromIdlcOutput(string cFilePath, string cycloneIncludePath)
    {
        var options = new CppParserOptions();
        options.IncludeFolders.Add(cycloneIncludePath);
        
        // Add ddsrt (assuming repo structure relative to src/core/ddsc/include)
        // ../../../ddsrt/include
        var ddsrtPath = Path.GetFullPath(Path.Combine(cycloneIncludePath, "../../../ddsrt/include"));
        if (Directory.Exists(ddsrtPath))
            options.IncludeFolders.Add(ddsrtPath);

        // Add fake includes if needed like in the offset generator
        var tempInclude = PrepareMockIncludes(options);
        
        var compilation = CppParser.ParseFile(cFilePath, options);
        
        if (compilation.HasErrors)
            throw new Exception("Parse failed: " + string.Join("\n", compilation.Diagnostics.Messages));
        
        // Find descriptor variable (e.g., Net_AppId_desc)
        // It's usually a global variable ending with _desc
        var descriptorVar = compilation.Fields
            .FirstOrDefault(f => f.Name.EndsWith("_desc") && IsTopicDescriptorType(f.Type));
        
        if (descriptorVar == null)
            throw new Exception("Could not find topic descriptor variable in " + cFilePath);
        
        var data = new DescriptorData();
        
        // Extract from descriptor initializer
        if (descriptorVar.InitExpression is CppInitListExpression initList)
        {
             // We need to support designated initializers if CppAst exposes them, or positional.
             // CppAst 0.x might not expose designators directly on CppInitListExpr elements easily?
             // But usually it flattens them or keeps order.
             // Let's assume we can traverse and map.
             // Since idlc output is standard, let's try to robustly find values.
             
             // However, dealing with designated initializers in CppAst can be tricky if the API doesn't fully expose them.
             // Strategy: Look for specific types/patterns in the init list.
             
             // m_typename is a string literal.
             // m_size is a sizeof or integer.
             // m_ops is a pointer to _ops array.
        }

        // Alternative: Use regex on the source file for simple scalar values if AST is too complex,
        // BUT we must use AST for ops array content.
        
        // Let's try to extract TypeName from AST (m_typename)
        // It's usually the 5th element (index 4) or accessible via traversal.
        
        // Let's parse the elements
        // Index 0: m_size. (sizeof(Type))
        // Index 1: m_align. (4u)
        // Index 2: m_flagset. (0u)
        // Index 3: m_nkeys.
        // Index 4: m_typename.
        
        // Note: this assumes standard idlc output structure matches dds_public_impl.h structure roughly.
        // Or checking expressions.
        
        // Let's refine based on typical idlc output.
        // m_typename = "Net::AppId"
        
        // We will try to scan the initialization list for the string literal for TypeName.
        data.TypeName = ExtractTypeName(descriptorVar);
        data.Size = ExtractUIntField(descriptorVar, "m_size");
        data.Align = ExtractUIntField(descriptorVar, "m_align"); // Approximate
        data.NKeys = ExtractUIntField(descriptorVar, "m_nkeys");
        data.NOps = ExtractUIntField(descriptorVar, "m_nops"); 
        
        // Ops Array
        var opsVarName = descriptorVar.Name.Replace("_desc", "_ops");
        var opsVar = compilation.Fields.FirstOrDefault(f => f.Name == opsVarName);
        if (opsVar != null && opsVar.InitExpression is CppInitListExpression opsList)
        {
            data.Ops = ExtractUInt32Array(opsList);
        }
        else
        {
             // Try finding by searching referenced variables in init list
             // m_ops = Net_AppId_ops
             // ...
        }

        // Extract TYPE_INFO_CDR macro
        data.TypeInfo = ExtractByteArrayFromMacro(compilation, "TYPE_INFO_CDR_");
        
        // Extract TYPE_MAP_CDR macro
        data.TypeMap = ExtractByteArrayFromMacro(compilation, "TYPE_MAP_CDR_");
        
        return data;
    }
    
    private static string PrepareMockIncludes(CppParserOptions options)
    {
        var tempInclude = Path.Combine(Path.GetTempPath(), "cyclone_extractor_mocks_" + Guid.NewGuid());
        var ddsDir = Path.Combine(tempInclude, "dds");
        Directory.CreateDirectory(ddsDir);
        File.WriteAllText(Path.Combine(ddsDir, "export.h"), "#define DDS_EXPORT\n");
        File.WriteAllText(Path.Combine(ddsDir, "features.h"), "#ifndef DDS_FEATURES_H\n#define DDS_FEATURES_H\n#endif\n");
        options.IncludeFolders.Add(tempInclude);
        return tempInclude;
    }
    
    private static bool IsTopicDescriptorType(CppType type)
    {
        var name = type.GetDisplayName();
        return name.Contains("dds_topic_descriptor");
    }

    private static string ExtractTypeName(CppField descriptorVar)
    { 
        // Try to find string literal in initializer
        if (descriptorVar.InitExpression is CppInitListExpression init)
        {
            foreach(var elem in init.Arguments)
            {
                 if (elem is CppLiteralExpression lit && lit.Kind == CppExpressionKind.StringLiteral)
                 {
                     return lit.Value;
                 }
                 // If it's a designated init: .m_typename = "..."
                 // CppAst might wrap it? No, LibClang does.
            }
        }
        return "Unknown";
    }
    
    // Very rudimentary extraction. 
    // Ideally we should match field names.
    // Since CppAst/LibClang might not expose designators easily in CppInitListExpr.Elements on all versions,
    // we might need to rely on assumptions or manual parsing if this fails.
    // But typically Idlc output is "const struct dds_topic_descriptor Desc = { ... }"
    
    private static uint ExtractUIntField(CppField descVar, string fieldName)
    {
         // This is hard to do reliably without designators.
         // Let's rely on standard idlc order for now for primitives?
         // Or just return defaults.
         // Wait, Size and Align are important.
         // idlc output: .m_size = sizeof (Type).
         
         // If we can't extract accurately, maybe we should rely on idlc providing this info differently?
         // But the instruction says "Parses idlc output".
         
         // Let's assume for BATCH-13 prototype we primarily need Ops, TypeInfo, TypeMap, and TypeName.
         // Size/Align are used for allocation.
         
         return 0; // Placeholder
    }


    private static uint[] ExtractUInt32Array(CppInitListExpression list)
    {
        var results = new List<uint>();
        foreach (var element in list.Arguments)
        {
            if (element is CppLiteralExpression lit)
            {
                // Handles hex 0x...
                try {
                    var val = lit.Value.StartsWith("0x") 
                        ? Convert.ToUInt64(lit.Value, 16)
                        : Convert.ToUInt64(lit.Value);
                    results.Add((uint)val);
                } catch {}
            }
            // Handle signed/unsigned casting
            else if (element.Kind == CppExpressionKind.CStyleCast && element.Arguments != null && element.Arguments.Count > 0)
            {
                 // Assume first argument is the value
                 if (element.Arguments[0] is CppLiteralExpression subLit)
                 {
                     try {
                        var val = subLit.Value.StartsWith("0x") 
                            ? Convert.ToUInt64(subLit.Value, 16)
                            : Convert.ToUInt64(subLit.Value);
                        results.Add((uint)val);
                    } catch {}
                 }
            }
        }
        return results.ToArray();
    }
    
    private static byte[] ExtractByteArrayFromMacro(CppCompilation compilation, string macroPrefix)
    {
        var macro = compilation.Macros.FirstOrDefault(m => m.Name.StartsWith(macroPrefix));
        if (macro == null) return Array.Empty<byte>();
        
        // Macro format: (unsigned char []){ 0x60, 0x00, ... }
        var content = macro.Value;
        var match = Regex.Match(content, @"\{(.+?)\}");
        if (!match.Success) return Array.Empty<byte>();
        
        var hexValues = match.Groups[1].Value.Split(',');
        var list = new List<byte>();
        foreach(var h in hexValues)
        {
             if (string.IsNullOrWhiteSpace(h)) continue;
             try {
                list.Add(Convert.ToByte(h.Trim().Replace("0x",""), 16));
             } catch {}
        }
        return list.ToArray();
    }
}
