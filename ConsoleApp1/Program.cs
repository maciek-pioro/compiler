
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;


public enum Type
{
    Integer,
    Boolean, 
    Real
}

//public class Variable {
//    public Type type;
//    public static int variablesCount = 0;
//    public string internalIdentifier;

//    public Variable(Type type)
//    {
//        this.type = type;
//        internalIdentifier = $"variable_{variablesCount}";
//        ++variablesCount;
//    }

//}

public abstract class Tree
{
    public Dictionary<String, Variable> variables;
    public Tree parent;
    public List<Tree> children;
    
    public abstract bool validate();
    
    abstract public String genCode();
}

public class Program: Tree
{
    private Tree declarations;
    private Tree instructions;

    public Program(Tree declarations, Tree instructions)
    {
        this.parent = null;
        this.declarations = declarations;
        this.instructions = instructions;
        this.variables = declarations.variables;
        this.children = new List<Tree>();
    }

    public override String genCode()
    {
        String declarations_code = declarations.genCode();
        String instructions_code = instructions.genCode();
        return
            @"@intFormat = constant [3 x i8] c""%d\00""" +
            "\n declare i32 @printf(i8*, ...)\n" +
            "declare i32 @scanf(i8 *, ...) \n" +
            "define void @main(){\n" +
            @"%l_double = alloca double
            %r_double = alloca double
            %l_int = alloca i32
            %r_int = alloca i32
            %l_boolean = alloca i1
            %r_boolean = alloca i1" +
            $"\n {declarations_code} " +
            $"\n {instructions_code} \n " +
            "ret void\n" +
            "}";
    }

    public override bool validate()
    {
        return true;
    }
}

public class DeclarationList : Tree
{

    public DeclarationList()
    {
        //this.parent = null;
        //this.declarations = declarations;
        //this.instructions = instructions;
        //this.variables = declarations.variables;
        this.children = new List<Tree>();
        this.variables = new Dictionary<string, Variable>();
    }

    public override String genCode()
    {
        //String declarations_code = declarations.genCode();
        string result = "";
        foreach(var child in children)
        {
            result += child.genCode();
        }
        return result;
    }

    public override bool validate()
    {
        return true;
    }
}

//public class Constant : Tree
//{
//    String value { get; set; }

//    public Constant(String value)
//    {
//        this.value = value;
//    }

//    public override string genCode()
//    {
//        Console.WriteLine(value);
//        return value;
//    }

//    public override bool validate()
//    {
//        throw new NotImplementedException();
//    }
//}

//public class Identifier : Tree
//{
//    String name { get; set; }

//    public Identifier(String name)
//    {
//        this.name = name;
//    }

//    public override string genCode()
//    {
//        Console.WriteLine(name);
//        return name;
//    }

//}

public class Variable : Tree
{
    public Type type;
    public string internalIdentifier;

    public Variable(Type type)
    {
        this.type = type;
        internalIdentifier = $"variable_{variablesCount}";
        ++variablesCount;
    }

    public override string genCode()
    {
        string typeConst = "i32";
        switch(type)
        {
            case Type.Boolean:
                {
                    typeConst = "i1";
                    break;
                }
            case Type.Real:
                {
                    typeConst = "double";
                    break;
                }
            case Type.Integer:
                {
                    typeConst = "i32";
                    break;
                }
        }
        return $"%{internalIdentifier} = alloca {typeConst}\n";
    }

    public override bool validate()
    {
        throw new NotImplementedException();
    }
}



//public class Printer
//{
//    public void Print(StringLiteral)
//    {

//    }
//}

public class StringLiteral
{

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

