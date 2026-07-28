using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SPTarkov.Core.Mods.Inspection;

/// <summary>
/// Reads mod component metadata from a DLL using only static metadata inspection. The assembly is never loaded and none
/// of its code ever executes.
///
/// A client plugin is a type carrying a BepInPlugin custom attribute, whose guid, name, and version are decoded from
/// the attribute value blob.
///
/// A server mod is a non-abstract type whose direct base type is named AbstractModMetadata, whose ModGuid, Name, and
/// Version are recovered by scanning its constructor IL for the literal initializer patterns the C# compiler emits.
/// </summary>
public static class DllMetadataReader
{
    private const string ServerMetadataBaseTypeName = "AbstractModMetadata";

    private const short Nop = 0x00;
    private const short Ldarg0 = 0x02;
    private const short Ldnull = 0x14;
    private const short LdcI4M1 = 0x15;
    private const short LdcI40 = 0x16;
    private const short LdcI48 = 0x1E;
    private const short LdcI4S = 0x1F;
    private const short LdcI4 = 0x20;
    private const short Dup = 0x25;
    private const short Pop = 0x26;
    private const short Call = 0x28;
    private const short BrTrueS = 0x2D;
    private const short BrTrue = 0x3A;
    private const short CallVirt = 0x6F;
    private const short Ldstr = 0x72;
    private const short Newobj = 0x73;
    private const short Stfld = 0x7D;
    private const short Ldtoken = 0xD0;

    private static readonly Dictionary<short, OperandType> OperandTypes = BuildOperandTypeTable();

    /// <summary>
    /// Reads every mod component declared in the DLL at the given path. Returns an empty list for files that are not
    /// valid .NET assemblies or contain no components.
    /// </summary>
    public static List<DllFinding> Read(string filePath, string relativePath)
    {
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            using PEReader peReader = new(stream);

            if (!peReader.HasMetadata)
            {
                return [];
            }

            MetadataReader metadata = peReader.GetMetadataReader();
            List<DllFinding> findings = [];

            foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
            {
                try
                {
                    TypeDefinition type = metadata.GetTypeDefinition(typeHandle);

                    DllFinding? clientPlugin = TryReadClientPlugin(metadata, type, relativePath);
                    if (clientPlugin is not null)
                    {
                        findings.Add(clientPlugin);
                    }

                    DllFinding? serverMod = TryReadServerMod(peReader, metadata, type, relativePath);
                    if (serverMod is not null)
                    {
                        findings.Add(serverMod);
                    }
                }
                catch (BadImageFormatException) { }
            }

            return findings;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Reads a client plugin finding from the type's BepInPlugin attribute, or null when the type carries none.
    /// </summary>
    private static DllFinding? TryReadClientPlugin(MetadataReader metadata, TypeDefinition type, string relativePath)
    {
        foreach (CustomAttributeHandle attributeHandle in type.GetCustomAttributes())
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(attributeHandle);

            if (!IsBepInPluginConstructor(metadata, attribute.Constructor))
            {
                continue;
            }

            (string? guid, string? name, string? version) = DecodeThreeStringArguments(metadata, attribute.Value);

            return new DllFinding(relativePath, DllComponentKind.Client, NullIfBlank(guid), NullIfBlank(name), NullIfBlank(version));
        }

        return null;
    }

    /// <summary>
    /// Reads a server mod finding from a non-abstract type directly extending AbstractModMetadata, recovering its
    /// literals from the constructor IL. Returns null when the type is not a server metadata carrier.
    /// </summary>
    private static DllFinding? TryReadServerMod(PEReader peReader, MetadataReader metadata, TypeDefinition type, string relativePath)
    {
        if (
            (type.Attributes & TypeAttributes.Abstract) != 0
            || (type.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface
        )
        {
            return null;
        }

        if (BaseTypeName(metadata, type.BaseType) != ServerMetadataBaseTypeName)
        {
            return null;
        }

        ConstructorLiterals literals = new();
        Version assemblyVersion = metadata.GetAssemblyDefinition().Version;

        foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
        {
            MethodDefinition method = metadata.GetMethodDefinition(methodHandle);

            if (metadata.GetString(method.Name) != ".ctor" || method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            ScanConstructorIl(peReader, metadata, method, literals, assemblyVersion);
        }

        return new DllFinding(
            relativePath,
            DllComponentKind.Server,
            NullIfBlank(literals.ModGuid),
            NullIfBlank(literals.Name),
            NullIfBlank(literals.Version)
        );
    }

    /// <summary>
    /// Walks a constructor's IL with a small literal-tracking stack model, capturing strings stored in the ModGuid,
    /// Name, and Version property backing fields. The model also recognizes the community idiom of deriving the
    /// version from the assembly version. Unrecognized instructions clear the model.
    /// </summary>
    private static void ScanConstructorIl(
        PEReader peReader,
        MetadataReader metadata,
        MethodDefinition method,
        ConstructorLiterals literals,
        Version assemblyVersion
    )
    {
        MethodBodyBlock body = peReader.GetMethodBody(method.RelativeVirtualAddress);
        BlobReader il = body.GetILReader();
        List<StackValue> stack = [];

        while (il.RemainingBytes > 0)
        {
            int firstByte = il.ReadByte();
            short opCode = firstByte == 0xFE && il.RemainingBytes > 0 ? (short)(0xFE00 | il.ReadByte()) : (short)firstByte;

            if (!OperandTypes.TryGetValue(opCode, out OperandType operandType))
            {
                return;
            }

            switch (opCode)
            {
                case Nop:
                    break;

                case Ldarg0:
                    stack.Add(new ThisValue());
                    break;

                case Ldnull:
                    stack.Add(new NullValue());
                    break;

                case >= LdcI4M1
                and <= LdcI48:
                    stack.Add(new IntValue(opCode - LdcI40));
                    break;

                case LdcI4S:
                    stack.Add(new IntValue(il.ReadSByte()));
                    break;

                case LdcI4:
                    stack.Add(new IntValue(il.ReadInt32()));
                    break;

                case Dup when stack.Count > 0:
                    stack.Add(stack[^1]);
                    break;

                case Pop when stack.Count > 0:
                    stack.RemoveAt(stack.Count - 1);
                    break;

                case Ldtoken:
                    il.ReadInt32();
                    stack.Add(new UnknownValue());
                    break;

                case Ldstr:
                    ReadUserString(metadata, il.ReadInt32(), stack);
                    break;

                case Call
                or CallVirt:
                    HandleCall(metadata, il.ReadInt32(), stack, assemblyVersion);
                    break;

                case BrTrueS
                or BrTrue:
                    HandleTrueBranch(ref il, opCode == BrTrueS, stack);
                    break;

                case Newobj:
                    HandleNewObject(metadata, il.ReadInt32(), stack);
                    break;

                case Stfld:
                    HandleStoreField(metadata, il.ReadInt32(), stack, literals);
                    break;

                default:
                    SkipOperand(ref il, operandType);
                    stack.Clear();
                    break;
            }
        }
    }

    /// <summary>
    /// Models a call instruction, popping arguments by signature and pushing an untracked result. Two members of the
    /// assembly-version idiom are tracked: AssemblyName.get_Version yields the assembly version marker, and calling
    /// ToString with a field count on that marker yields the string the runtime would produce from the assembly
    /// version stored in the DLL's own metadata.
    /// </summary>
    private static void HandleCall(MetadataReader metadata, int token, List<StackValue> stack, Version assemblyVersion)
    {
        EntityHandle method = MetadataTokens.EntityHandle(token);
        string? typeName = MethodDeclaringTypeName(metadata, method);
        string? methodName = MethodName(metadata, method);
        MethodShape? shape = ReadMethodShape(metadata, method);

        if (typeName is null || methodName is null || shape is null)
        {
            stack.Clear();
            return;
        }

        int popCount = shape.ParameterCount + (shape.IsInstance ? 1 : 0);

        if (popCount > stack.Count)
        {
            stack.Clear();

            if (!shape.ReturnsVoid)
            {
                stack.Add(new UnknownValue());
            }

            return;
        }

        List<StackValue> operands = stack.GetRange(stack.Count - popCount, popCount);
        stack.RemoveRange(stack.Count - popCount, popCount);

        if (methodName == "get_Version" && typeName == "AssemblyName")
        {
            stack.Add(new AssemblyVersionValue());
            return;
        }

        if (
            methodName == "ToString"
            && typeName == "Version"
            && popCount == 2
            && operands[0] is AssemblyVersionValue
            && operands[1] is IntValue fieldCount
        )
        {
            stack.Add(new StringValue(FormatAssemblyVersion(assemblyVersion, fieldCount.Number)));
            return;
        }

        if (!shape.ReturnsVoid)
        {
            stack.Add(new UnknownValue());
        }
    }

    /// <summary>
    /// Models a brtrue branch: the branch is taken when the popped condition is the assembly version marker, and any
    /// other condition clears the model.
    /// </summary>
    private static void HandleTrueBranch(ref BlobReader il, bool shortForm, List<StackValue> stack)
    {
        int offset = shortForm ? il.ReadSByte() : il.ReadInt32();
        StackValue? condition = stack.Count > 0 ? stack[^1] : null;

        if (stack.Count > 0)
        {
            stack.RemoveAt(stack.Count - 1);
        }

        if (condition is AssemblyVersionValue && offset > 0)
        {
            il.Offset += offset;
            return;
        }

        stack.Clear();
    }

    /// <summary>Formats the assembly version to the given field count, as System.Version.ToString(int) does.</summary>
    private static string FormatAssemblyVersion(Version version, int fieldCount)
    {
        return fieldCount switch
        {
            1 => $"{version.Major}",
            2 => $"{version.Major}.{version.Minor}",
            4 => $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}",
            _ => $"{version.Major}.{version.Minor}.{version.Build}",
        };
    }

    /// <summary>Pushes the user string a ldstr token refers to, clearing the model when the token is not one.</summary>
    private static void ReadUserString(MetadataReader metadata, int token, List<StackValue> stack)
    {
        Handle handle = MetadataTokens.Handle(token);

        if (handle.Kind != HandleKind.UserString)
        {
            stack.Clear();
            return;
        }

        stack.Add(new StringValue(metadata.GetUserString((UserStringHandle)handle)));
    }

    /// <summary>
    /// Models a newobj instruction. A constructor on a type named Version turns its tracked literal arguments into the
    /// version string they would produce; any other constructor yields an untracked value.
    /// </summary>
    private static void HandleNewObject(MetadataReader metadata, int token, List<StackValue> stack)
    {
        EntityHandle constructor = MetadataTokens.EntityHandle(token);
        string? typeName = MethodDeclaringTypeName(metadata, constructor);
        int? parameterCount = ReadMethodShape(metadata, constructor)?.ParameterCount;

        if (typeName is null || parameterCount is null || parameterCount.Value > stack.Count)
        {
            stack.Clear();
            stack.Add(new UnknownValue());
            return;
        }

        List<StackValue> arguments = stack.GetRange(stack.Count - parameterCount.Value, parameterCount.Value);
        stack.RemoveRange(stack.Count - parameterCount.Value, parameterCount.Value);

        if (typeName == "Version")
        {
            string? version = ComposeVersion(arguments);
            stack.Add(version is null ? new UnknownValue() : new StringValue(version));
            return;
        }

        stack.Add(new UnknownValue());
    }

    /// <summary>
    /// Composes the version string a Version constructor call would produce from its tracked arguments: either a
    /// string literal (optionally followed by a loose flag), or major/minor/patch integers with optional pre-release
    /// and build strings. Returns null when the arguments do not form a recognized literal shape.
    /// </summary>
    private static string? ComposeVersion(List<StackValue> arguments)
    {
        if (
            arguments.Count >= 1
            && arguments[0] is StringValue text
            && arguments.Skip(1).All(argument => argument is IntValue or NullValue)
        )
        {
            return text.Text;
        }

        if (arguments.Count >= 3 && arguments[0] is IntValue major && arguments[1] is IntValue minor && arguments[2] is IntValue patch)
        {
            string version = $"{major.Number}.{minor.Number}.{patch.Number}";

            if (arguments.Count >= 4 && arguments[3] is StringValue preRelease && preRelease.Text.Length > 0)
            {
                version += "-" + preRelease.Text;
            }

            if (arguments.Count >= 5 && arguments[4] is StringValue build && build.Text.Length > 0)
            {
                version += "+" + build.Text;
            }

            return version;
        }

        return null;
    }

    /// <summary>
    /// Models a stfld instruction, capturing a tracked string stored to the ModGuid, Name, or Version backing field.
    /// </summary>
    private static void HandleStoreField(MetadataReader metadata, int token, List<StackValue> stack, ConstructorLiterals literals)
    {
        string? fieldName = FieldName(metadata, MetadataTokens.EntityHandle(token));

        if (stack.Count < 2)
        {
            stack.Clear();
            return;
        }

        StackValue value = stack[^1];
        stack.RemoveRange(stack.Count - 2, 2);

        if (fieldName is null || value is not StringValue text)
        {
            return;
        }

        switch (fieldName)
        {
            case "<ModGuid>k__BackingField":
                literals.ModGuid ??= text.Text;
                break;

            case "<Name>k__BackingField":
                literals.Name ??= text.Text;
                break;

            case "<Version>k__BackingField":
                literals.Version ??= text.Text;
                break;
        }
    }

    /// <summary>Advances the IL reader past an instruction operand it does not model.</summary>
    private static void SkipOperand(ref BlobReader il, OperandType operandType)
    {
        if (operandType == OperandType.InlineSwitch)
        {
            int count = il.ReadInt32();
            il.Offset += count * 4;
            return;
        }

        il.Offset += operandType switch
        {
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget
            or OperandType.InlineField
            or OperandType.InlineI
            or OperandType.InlineMethod
            or OperandType.InlineSig
            or OperandType.InlineString
            or OperandType.InlineTok
            or OperandType.InlineType
            or OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            _ => 0,
        };
    }

    /// <summary>
    /// Determines whether a custom attribute constructor belongs to a BepInPlugin attribute type taking three strings.
    /// </summary>
    private static bool IsBepInPluginConstructor(MetadataReader metadata, EntityHandle constructor)
    {
        string? typeName = MethodDeclaringTypeName(metadata, constructor);

        if (typeName is null || !typeName.Contains("BepInPlugin", StringComparison.Ordinal))
        {
            return false;
        }

        BlobHandle signature = MethodSignature(metadata, constructor);
        if (signature.IsNil)
        {
            return false;
        }

        BlobReader reader = metadata.GetBlobReader(signature);
        SignatureHeader header = reader.ReadSignatureHeader();

        if (header.Kind != SignatureKind.Method || header.IsGeneric)
        {
            return false;
        }

        if (reader.ReadCompressedInteger() < 3)
        {
            return false;
        }

        if (reader.ReadCompressedInteger() != (int)SignatureTypeCode.Void)
        {
            return false;
        }

        for (int index = 0; index < 3; index++)
        {
            if (reader.ReadCompressedInteger() != (int)SignatureTypeCode.String)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Decodes the three leading string values from a custom attribute value blob.</summary>
    private static (string? Guid, string? Name, string? Version) DecodeThreeStringArguments(MetadataReader metadata, BlobHandle value)
    {
        try
        {
            BlobReader reader = metadata.GetBlobReader(value);

            if (reader.ReadUInt16() != 1)
            {
                return (null, null, null);
            }

            return (reader.ReadSerializedString(), reader.ReadSerializedString(), reader.ReadSerializedString());
        }
        catch (BadImageFormatException)
        {
            return (null, null, null);
        }
    }

    /// <summary>Resolves the simple name of the type declaring a method handle.</summary>
    private static string? MethodDeclaringTypeName(MetadataReader metadata, EntityHandle method)
    {
        switch (method.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference member = metadata.GetMemberReference((MemberReferenceHandle)method);

                return member.Parent.Kind switch
                {
                    HandleKind.TypeReference => metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)member.Parent).Name),
                    HandleKind.TypeDefinition => metadata.GetString(metadata.GetTypeDefinition((TypeDefinitionHandle)member.Parent).Name),
                    _ => null,
                };

            case HandleKind.MethodDefinition:
                MethodDefinition definition = metadata.GetMethodDefinition((MethodDefinitionHandle)method);

                return metadata.GetString(metadata.GetTypeDefinition(definition.GetDeclaringType()).Name);

            default:
                return null;
        }
    }

    /// <summary>Resolves the simple name of a method handle.</summary>
    private static string? MethodName(MetadataReader metadata, EntityHandle method)
    {
        return method.Kind switch
        {
            HandleKind.MemberReference => metadata.GetString(metadata.GetMemberReference((MemberReferenceHandle)method).Name),
            HandleKind.MethodDefinition => metadata.GetString(metadata.GetMethodDefinition((MethodDefinitionHandle)method).Name),
            _ => null,
        };
    }

    /// <summary>Resolves the signature blob of a method handle.</summary>
    private static BlobHandle MethodSignature(MetadataReader metadata, EntityHandle method)
    {
        return method.Kind switch
        {
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)method).Signature,
            HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)method).Signature,
            _ => default,
        };
    }

    /// <summary>Reads a method's parameter count, instance flag, and void-return flag from its signature.</summary>
    private static MethodShape? ReadMethodShape(MetadataReader metadata, EntityHandle method)
    {
        BlobHandle signature = MethodSignature(metadata, method);

        if (signature.IsNil)
        {
            return null;
        }

        BlobReader reader = metadata.GetBlobReader(signature);
        SignatureHeader header = reader.ReadSignatureHeader();

        if (header.Kind != SignatureKind.Method)
        {
            return null;
        }

        if (header.IsGeneric)
        {
            reader.ReadCompressedInteger();
        }

        int parameterCount = reader.ReadCompressedInteger();
        bool returnsVoid = reader.ReadCompressedInteger() == (int)SignatureTypeCode.Void;

        return new MethodShape(parameterCount, header.IsInstance, returnsVoid);
    }

    /// <summary>Resolves the simple name of a field handle.</summary>
    private static string? FieldName(MetadataReader metadata, EntityHandle field)
    {
        return field.Kind switch
        {
            HandleKind.FieldDefinition => metadata.GetString(metadata.GetFieldDefinition((FieldDefinitionHandle)field).Name),
            HandleKind.MemberReference => metadata.GetString(metadata.GetMemberReference((MemberReferenceHandle)field).Name),
            _ => null,
        };
    }

    /// <summary>Resolves the simple name of a type definition's base type handle.</summary>
    private static string? BaseTypeName(MetadataReader metadata, EntityHandle baseType)
    {
        if (baseType.IsNil)
        {
            return null;
        }

        return baseType.Kind switch
        {
            HandleKind.TypeReference => metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)baseType).Name),
            HandleKind.TypeDefinition => metadata.GetString(metadata.GetTypeDefinition((TypeDefinitionHandle)baseType).Name),
            _ => null,
        };
    }

    /// <summary>Maps every IL opcode to its operand type.</summary>
    private static Dictionary<short, OperandType> BuildOperandTypeTable()
    {
        Dictionary<short, OperandType> table = [];

        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                table[opCode.Value] = opCode.OperandType;
            }
        }

        return table;
    }

    /// <summary>Normalizes a recovered value, treating blank strings as unreadable.</summary>
    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>The literals recovered from a server metadata type's constructors.</summary>
    private sealed class ConstructorLiterals
    {
        public string? ModGuid;

        public string? Name;

        public string? Version;
    }

    /// <summary>A value on the tracked IL evaluation stack.</summary>
    private abstract record StackValue;

    /// <summary>A tracked string literal.</summary>
    private sealed record StringValue(string Text) : StackValue;

    /// <summary>A tracked 32-bit integer literal.</summary>
    private sealed record IntValue(int Number) : StackValue;

    /// <summary>A tracked null literal.</summary>
    private sealed record NullValue : StackValue;

    /// <summary>The this reference loaded by ldarg.0.</summary>
    private sealed record ThisValue : StackValue;

    /// <summary>A value the model cannot track.</summary>
    private sealed record UnknownValue : StackValue;

    /// <summary>The assembly version value produced by AssemblyName.get_Version.</summary>
    private sealed record AssemblyVersionValue : StackValue;

    /// <summary>The parts of a method signature the stack model needs.</summary>
    private sealed record MethodShape(int ParameterCount, bool IsInstance, bool ReturnsVoid);
}
