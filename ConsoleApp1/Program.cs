
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;


public enum Type
{
    Integer,
    Boolean, 
    Double
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

    public Tree()
    {
        variables = new Dictionary<string, Variable>();
        resultVariable = $"tmp_{temporaryVariablesCount}";
        this.children = new List<Tree>();
        ++temporaryVariablesCount;
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
    
    abstract public String genCode();
}

public class Program: Tree
{
    private Tree declarations { get { return children[0]; } }
    private Tree instructions { get { return children[1]; } }

    public Program(Tree declarations, Tree instructions)
    {
        this.children.Add(declarations);
        this.children.Add(instructions);
        this.variables = declarations.variables;
    }

    public override String genCode()
    {
        String declarations_code = declarations.genCode();
        String instructions_code = instructions.genCode();
        return
            @"@i32HexFormat = constant [3 x i8] c""%X\00""" + "\n" +
            @"@i32Format = constant [3 x i8] c""%d\00""" + "\n" +
            @"@doubleFormat = constant [3 x i8] c""%f\00""" +
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
        if(value.Substring(0, 2).Equals("0X") || value.Substring(0, 2).Equals("0x"))
            this.value = Convert.ToInt32(value, 16).ToString();

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
        switch(children[0].type)
        {
            case Type.Boolean:
                {
                    return "";
                }
            case Type.Double:
                {
                    format = "@doubleFormat";
                    break;
                }
            case Type.Integer:
                {
                    if (isHex)
                    {
                        format = "@i32HexFormat";
                    }
                    else
                    {
                        format = "@i32Format";
                    }
                    break;
                }
        }
        Console.WriteLine($"Format {format}");
        var typeString = children[0].type.ToLLVMString();
        string result = $"call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * {format} to i8 *), {typeString} %{childResult})";
        return childCode + "\n" + result;

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
        Console.WriteLine("Got the variable");
        Console.WriteLine("Got the variable");
        Console.WriteLine("Got the variable");
        Console.WriteLine("Got the variable");
        Console.WriteLine("Got the variable");
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

