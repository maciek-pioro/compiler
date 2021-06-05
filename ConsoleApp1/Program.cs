
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;


public enum Type
{
    Integer,
    Boolean, 
    Double,
    String
}

public static class TypeStringer
{
    public static string ToLLVMString(this Type type)
    {
        switch (type)
        {
            case Type.Boolean:
                return "i1";
            case Type.Double:
                return "double";
            case Type.Integer:
                return "i32";
            default:
                return "ERROR";
        }
    }

}

public abstract class Tree
{
    public Dictionary<String, Variable> variables;
    public Tree parent;
    public List<Tree> children;
    public Type type;
    public string resultVariable;
    private static int temporaryVariablesCount = 0;
    protected int uniqueId;
    private static int ids = 0;

    public Tree()
    {
        variables = new Dictionary<string, Variable>();
        resultVariable = $"tmp_{temporaryVariablesCount}";
        this.children = new List<Tree>();
        ++temporaryVariablesCount;
        uniqueId = ids++;
    }

    public Variable getVariable(string identifier)
    {
        Console.WriteLine(this.GetType());
        if (variables.TryGetValue(identifier, out Variable variable))
        {
            Console.WriteLine($"Found {identifier}");
            return variable;
        }
        return parent?.getVariable(identifier);
    }

    public override string ToString()
    {
        string typeConst = type.ToLLVMString();
        return $"{typeConst} %{resultVariable}";
    }

    public void setParent(Tree tree = null)
    {
        this.parent = tree;
        foreach(var child in children)
        {
            child.setParent(this);
        }
    }

    public virtual bool validate()
    {
        bool result = true;
        foreach(var child in children)
        {
            result = result && child.validate();
        }
        return result;
    }

    public virtual List<Tree> hoistStrings()
    {
        var result = new List<Tree>();
        foreach (var child in children)
        {
            result.AddRange(child.hoistStrings());
        }
        return result;
    } 
    
    abstract public String genCode();
}

public class Program: Tree
{
    public List<Tree> stringNodes;

    private Tree declarations { get { return children[0]; } }
    private Tree instructions { get { return children[1]; } }

    public Program(Tree declarations, Tree instructions)
    {
        this.children.Add(declarations);
        this.children.Add(instructions);
        this.variables = declarations.variables;
        this.stringNodes = new List<Tree>();
    }
   
    public override String genCode()
    {
        String declarations_code = declarations.genCode();
        String instructions_code = instructions.genCode();
        String strings_code = "";
        foreach(var node in stringNodes)
        {
            strings_code = strings_code + "\n" + node.genCode();
        }
        return
            @"@i32HexFormat = constant [5 x i8] c""0X%X\00""" + "\n" +
            @"@i32Format = constant [3 x i8] c""%d\00""" + "\n" +
            @"@doubleFormat = constant [3 x i8] c""%f\00""" + "\n" +
            @"@stringFormat = constant [3 x i8] c""%s\00""" + "\n" +
            @"@trueString = constant [5 x i8] c""True\00""" + "\n" +
            @"@falseString = constant [6 x i8] c""False\00""" + "\n" +
            @"@readInt = constant[3 x i8] c""%d\00""" + "\n" +
            @"@readIntHex = constant[3 x i8] c""%X\00""" + "\n" +
            @"@readDouble = constant[4 x i8] c""%lf\00""" + "\n" +
            strings_code + "\n" +
            "\n declare i32 @printf(i8*, ...)\n" +
            "declare i32 @scanf(i8 *, ...) \n" +
            "define i32 @main(){\n" +
            @"%l_double = alloca double
            %r_double = alloca double
            %result_double = alloca double
            %l_i32 = alloca i32
            %r_i32 = alloca i32
            %result_i32 = alloca i32
            %l_i1 = alloca i1
            %r_i1 = alloca i1
            %result_i1 = alloca i1" +
            $"\n {declarations_code} " +
            $"\n {instructions_code} \n " +
            "ret i32 0\n" +
            "}";
    }

    public override List<Tree> hoistStrings()
    {
        var strings = base.hoistStrings();
        this.stringNodes = strings;
        return null;
    }

    //public override bool validate()
    //{
    //    bool valid = true;
    //    foreach(var child in children)
    //    {
    //        valid = valid && child.validate();
    //    }
    //    return valid;
    //}
}

public class Literal : Tree
{
    string value;
    public Literal(Type type, string value)
    {
        this.type = type;
        if(this.type == Type.Boolean)
        {
            if (value == "true") value = "1";
            if (value == "false") value = "0";
        }
        if (this.type == Type.Integer) {
            if (value.Substring(0, 2).Equals("0X") || value.Substring(0, 2).Equals("0x"))
                value = Convert.ToInt32(value, 16).ToString();
        }
        this.value = value;
    }

    public override string genCode()
    {
        string typeString = this.type.ToLLVMString();
        return $"store {typeString} {value}, {typeString}* %result_{typeString}\n" +
            $"%{this.resultVariable} = load {typeString}, {typeString}* %result_{typeString}\n";
    }

    public override bool validate()
    {
        return true;
    }
}
public class DeclarationList : Tree
{

    public DeclarationList(): base()
    {
        children = new List<Tree>();
        variables = new Dictionary<string, Variable>();
    }

    public override String genCode()
    {
        string result = "";
        foreach(var child in children)
        {
            result += child.genCode();
        }
        return result;
    }

    public override bool validate()
    {
        foreach(var child in children)
        {
            if (variables.TryGetValue(((Variable)child).name, out var variable))
            {
                Console.WriteLine($"variable {variable.name} already declared");
                return false;
            } else
            {
                variables.Add(((Variable)child).name, (Variable)child);
            }
        }
        return true;
    }
}


public class InstructionList : Tree
{

    public InstructionList() : base() { }

    public override String genCode()
    {
        string result = "";
        foreach (var child in children)
        {
            result += child.genCode();
        }
        return result;
    }

    public override bool validate()
    {
        bool result = true;
        foreach (var child in children)
        {
            result = result && child.validate();
        }
        return true;
    }
}


public class Variable : Tree
{
    public string internalIdentifier;
    public string name;
    private static int variablesCount = 0;

    public Variable(Type type, string name): base()
    {
        this.name = name;
        this.type = type;
        internalIdentifier = $"variable_{variablesCount}";
        ++variablesCount;
    }

    public override string genCode()
    {
        string typeConst = type.ToLLVMString();
        return $"%{internalIdentifier} = alloca {typeConst}\n";
    }

    public override string ToString()
    {
        string typeConst = type.ToLLVMString();
        return $"{typeConst}* %{internalIdentifier}";
    }

    public override bool validate()
    { 
        return true;
    }
}

public class Write : Tree
{
    private bool isHex;
    public Write(Tree child, bool isHex = false): base()
    {
        this.children.Add(child);
        this.isHex = isHex;
    }

    public override string genCode()
    {
        var childCode = children[0].genCode();
        string childResult = children[0].resultVariable;
        string format = null;
        string result = "";
        var typeString = children[0].type.ToLLVMString();
        switch(children[0].type)
        {
            case Type.Boolean:
                {
                    result = "";
                    result += $"br {children[0]}, label %write_true_{this.uniqueId}, label %write_false_{this.uniqueId} \n";
                    result += $"write_true_{this.uniqueId}:\n";
                    result += $"call i32(i8*, ...) @printf(i8 * bitcast([5 x i8] * @trueString to i8 *))\n";
                    result += $"br label %write_end_{this.uniqueId}\n";
                    result += $"write_false_{this.uniqueId}:\n";
                    result += $"call i32(i8*, ...) @printf(i8 * bitcast([6 x i8] * @falseString to i8 *))\n";
                    result += $"br label %write_end_{this.uniqueId}\n";
                    result += $"write_end_{this.uniqueId}: \n";
                    return childCode + "\n" + result;
                }
            case Type.Double:
                {
                    format = "[3 x i8] * @doubleFormat to i8 *";
                    break;
                }
            case Type.Integer:
                {
                    if (isHex)
                    {
                        format = "[5 x i8] * @i32HexFormat to i8 *";
                    }
                    else
                    {
                        format = "[3 x i8] * @i32Format to i8 *";
                    }
                    break;
                }
            case Type.String:
                {
                    StringLiteral childAsString = (StringLiteral)children[0];
                    return $"call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), {childAsString.asArgument()})\n";
                }
        }
        result = $"call i32(i8*, ...) @printf(i8 * bitcast({format}), {typeString} %{childResult})\n";
        return childCode + "\n" + result;

    }

    public override bool validate()
    {
        bool result = true;
        bool baseResult = base.validate();
        if (isHex && children[0].type != Type.Integer)
        {
            Console.WriteLine("Only integer values can be displayed as hex");
            result = false;
        }
        return baseResult && result;
    }

}
public class Read : Tree
{
    private Variable variable;
    private string identifier;
    private bool isHex;
    public Read(string identifier, bool isHex = false) : base()
    {
        this.identifier = identifier;
        this.isHex = isHex;
    }

    public override string genCode()
    {
        string format = null;
        int length = 3;
        switch (variable.type)
        {
            case Type.Double:
                {
                    format = "readDouble";
                    length = 4;
                    break;
                }
            case Type.Integer:
                {
                    if (isHex)
                    {
                        format = "readIntHex";
                    }
                    else
                    {
                        format = "readInt";
                    }
                    break;
                }
        }
        return $"call i32 (i8*, ...) @scanf(i8* bitcast ([{length} x i8]* @{format} to i8*), {variable})\n";
    }

    public override bool validate()
    {
        var variable = getVariable(identifier);
        bool result = true;
        if (variable is null)
        {
            Console.WriteLine($"Undeclared identifier: {identifier}");
            result = false;
        }
        this.variable = variable;
        if (variable.type != Type.Integer && variable.type != Type.Double)
        {
            Console.WriteLine($"Can only write to integer and double");
            result = false;
        }
        if (variable.type != Type.Integer && isHex)
        {
            Console.WriteLine($"Can only write hex to int");
            result = false;
        }
        return result;
    }

}

public class StringLiteral : Tree
{
    private static int stringCounter = 0;
    public string stringIdentifier;
    public string value;
    public int effectiveLength;


    public StringLiteral(string value) :base() {
        stringIdentifier = $"@string_{stringCounter}";
        ++stringCounter;
        this.type = Type.String;
        value = value.Trim('"');
        var parts = value.Split(new string[] { @"\n" }, StringSplitOptions.None);
        int newLines = parts.Length - 1;
        this.value = String.Join(@"\0A", parts);
        effectiveLength = this.value.Length - 2 * newLines + 1;
    }

    public override string genCode()
    {
        return $"{stringIdentifier} = constant[{effectiveLength} x i8] c\"{value}\\00\"";
    }

    public string asArgument()
    {
        return $"i8* bitcast([{effectiveLength} x i8]* {stringIdentifier} to i8*)";
    }

    public override List<Tree> hoistStrings()
    {
        return new List<Tree>{ this };
    }
}

public class Assign: Tree
{
    string identifier;
    
    public Assign(string identifier, Tree rTree): base()
    {
        this.identifier = identifier;
        this.children.Add(rTree);
    }

    public override string genCode()
    {
        Tree rTree = children[0];
        var variable = getVariable(identifier);
        resultVariable = rTree.resultVariable;
        return rTree.genCode() + "\n" + $"store {rTree}, {variable}\n";
    }

    public override bool validate()
    {
        Tree rTree = children[0];
        bool result = true;
        var variable = getVariable(identifier);
        if (variable is null)
        {
            Console.WriteLine($"Undeclared identifier: {identifier}");
            result = false;
        }
        result = result && rTree.validate();
        return result;
    }
}

// Używane, kiedy jest odwołanie do wartości zmiennej, czyli zawsze oprócz przypisania do zmiennej
public class Identifier : Tree
{
    string identifier;
    public Identifier(string identifier): base()
    {
        this.identifier = identifier;
    }

    override public string genCode()
    {
        var variable = getVariable(identifier);
        Type type = variable.type;
        string typeString = type.ToLLVMString();
        Console.WriteLine(this.type);
        Console.WriteLine("asdasdasdasd");
        return $"%{resultVariable} = load {typeString}, {variable}";
    }


    public override bool validate()
    {
        var variable = getVariable(identifier);
        if (variable is null)
        {
            Console.WriteLine($"Undeclared identifier: {identifier}");
            return false;
        }
        this.type = variable.type;
        return true;
    }
}


public class Compiler
{

    public static int errors = 0;

    public static List<string> source;

    //public static Dictionary<String, Tree> variables;
    //public static List<Identifier> identifiers1;
    //public static List<Identifier> identifiers2;

    // arg[0] określa plik źródłowy
    // pozostałe argumenty są ignorowane
    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nSingle-Pass CIL Code Generator for Multiline Calculator - Gardens Point");
        if (args.Length >= 1)
            file = args[0];
        else
        {
            Console.Write("\nsource file:  ");
            file = Console.ReadLine();
        }
        try
        {
            var sr = new StreamReader(file);
            string str = sr.ReadToEnd();
            sr.Close();
            Compiler.source = new System.Collections.Generic.List<string>(str.Split(new string[] { "\r\n" }, System.StringSplitOptions.None));
            source = new FileStream(file, FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
            return 1;
        }
        Scanner scanner = new Scanner(source);
        Parser parser = new Parser(scanner);
        Console.WriteLine();
        sw = new StreamWriter(Console.OpenStandardOutput());
        sw.AutoFlush = true;
        parser.Parse();
        var Program = parser.head;
        //if (!Program.validate())
        //{
        //    return 1;
        //}
        Program.setParent();
        if(!Program.validate()) {
            Console.WriteLine("Errors detected :(, aborting");
            return 1;
        }
        Program.hoistStrings();
        Console.WriteLine(parser.head.genCode());
        sw.Close();
        source.Close();
        if (errors == 0)
            //Console.WriteLine("  compilation successful\n");
            Console.WriteLine("\n");
        else
                {
            Console.WriteLine($"\n  {errors} errors detected\n");
            File.Delete(file + ".il");
        }

        Console.ReadKey();

        return errors == 0 ? 0 : 2;
    }

    public static void EmitCode(string instr = null)
    {
        sw.WriteLine(instr);
    }

    public static void EmitCode(string instr, params object[] args)
    {
        sw.WriteLine(instr, args);
    }

    private static StreamWriter sw;

    private static void GenProlog()
    {
        EmitCode("define void @main() {");
    }

    private static void GenEpilog()
    {
        EmitCode("}");
    }

}

