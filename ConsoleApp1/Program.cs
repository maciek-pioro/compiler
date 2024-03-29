﻿
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

public class TypeHelper
{
    private TypeHelper() { }
    
    public static Type getMoreGeneralType(Type l, Type r)
    {
        if (l == Type.Double || r == Type.Double) return Type.Double;
        if (l == Type.Integer || r == Type.Integer) return Type.Integer;
        return Type.Boolean;
    }
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
    public int lineNumber;
    public Dictionary<String, Variable> variables;
    public Tree parent;
    public List<Tree> children;
    public Type type;
    public string resultVariable;
    private static int temporaryVariablesCount = 0;
    protected int uniqueId;
    private static int ids = 0;

    public Tree(int lineNumber)
    {
        variables = new Dictionary<string, Variable>();
        resultVariable = $"tmp_{temporaryVariablesCount}";
        this.children = new List<Tree>();
        ++temporaryVariablesCount;
        uniqueId = ids++;
        this.lineNumber = lineNumber;
    }

    public Variable getVariable(string identifier)
    {
        if (variables.TryGetValue(identifier, out Variable variable))
        {
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
            result = child.validate() && result;
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

    public virtual List<Tree> hoistDeclarations()
    {
        var result = new List<Tree>();
        foreach (var child in children)
        {
            result.AddRange(child.hoistDeclarations());
        }
        return result;
    }

    public virtual void markTypes(Type? type = null)
    {
        foreach(var child in children)
        {
            child.markTypes(type);
        }
    }

    abstract public String genCode();
}

public class Wrapper : Tree
{
    bool isExplicit;

    public Wrapper (Type outType, Tree child, int lineNumber, bool isExplicit = false): base(lineNumber)
    {
        children.Add(child);
        type = outType;
        this.isExplicit = isExplicit;
    }

    public override void markTypes(Type? type = null)
    {
        if (!isExplicit) this.type = (Type)type;
        else children[0].markTypes(this.type);
    }

    public override string genCode()
    {
        var childCode = "";
        Type inType = children[0].type;
        Type outType = type;
        switch (inType, outType)
        {
            case (Type.Double, Type.Integer):
            {
                childCode = children[0].genCode();
                return childCode + "\n" + $"%{resultVariable} = fptosi {children[0]} to i32\n";
            }
            case (Type.Integer, Type.Double):
            {
                childCode = children[0].genCode();
                return childCode + "\n" + $"%{resultVariable} = sitofp {children[0]} to double\n";
            }
            case (Type.Boolean, Type.Integer):
            {
                childCode = children[0].genCode();
                return childCode + "\n" + $"%{resultVariable} = select {children[0]}, i32 1, i32 0\n";
            }
        }
        childCode = children[0].genCode();
        resultVariable = children[0].resultVariable;
        return childCode;
    }

    public override bool validate()
    {
        bool result = base.validate();
        Type inType = children[0].type;
        Type outType = type;
        switch (inType, outType)
        {
            case (Type.Boolean, Type.Double):
                {
                    result = false;
                    Console.WriteLine($"Line {lineNumber}: Cannot convert boolean to double");
                    break;
                }
            case (Type.Double, Type.Boolean):
                {
                    result = false;
                    Console.WriteLine($"Line {lineNumber}: Cannot convert double to boolean");
                    break;
                }
            case (Type.Integer, Type.Boolean):
                {
                    result = false;
                    Console.WriteLine($"Line {lineNumber}: Cannot convert integer to boolean");
                    break;
                }
            case (Type.Boolean, Type.Integer):
                {
                    result = isExplicit && result;
                    if(!isExplicit)
                    {
                        Console.WriteLine($"Line {lineNumber}: Cannot convert boolean to integer implicitly");
                    }
                    break;
                }
            case (Type.Integer, Type.Double):
                {
                    break;
                }
            case (Type.Double, Type.Integer):
                {
                    result = isExplicit && result;
                    if (!isExplicit)
                    {
                        Console.WriteLine($"Line {lineNumber}: Cannot convert double to integer implicitly");
                    }
                    break;
                }
        }
        return result;
    }
}

public class Program: Tree
{
    public List<Tree> stringNodes;
    public List<Tree> declarationNodes;

    public Program(Tree block): base(0)
    {
        this.children.Add(block);
        this.stringNodes = new List<Tree>();
        this.declarationNodes = new List<Tree>();
    }
   
    public override String genCode()
    {
        String stringsCode = "";
        foreach(var node in stringNodes)
        {
            stringsCode = stringsCode + "\n" + node.genCode();
        }
        String declarationsCode = "";
        foreach (var node in declarationNodes)
        {
            declarationsCode = declarationsCode + "\n" + node.genCode();
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
            stringsCode + "\n" +
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
            declarationsCode + "\n" +
            children[0].genCode() +
            "ret i32 0\n" +
            "}";
    }

    public override List<Tree> hoistStrings()
    {
        var strings = base.hoistStrings();
        this.stringNodes = strings;
        return null;
    }

    public override List<Tree> hoistDeclarations()
    {
        var declarations = base.hoistDeclarations();
        this.declarationNodes = declarations;
        return null;
    }


}

public class Block : Tree
{
    private Tree declarations { get { return children[0]; } }
    private Tree instructions { get { return children[1]; } }

    public Block(Tree declarations, Tree instructions, int lineNumber): base(0)
    {
        this.children.Add(declarations);
        this.children.Add(instructions);
        this.variables = declarations.variables;
    }

    public override String genCode()
    {
        //String declarations_code = declarations.genCode();
        String instructions_code = instructions.genCode();
        return instructions_code + "\n";
    }
}


public class Literal : Tree
{
    string value;
    public Literal(Type type, string value, int lineNumber): base(lineNumber)
    {
        this.type = type;
        if(this.type == Type.Boolean)
        {
            if (value == "true") value = "1";
            if (value == "false") value = "0";
        }
        if (this.type == Type.Integer) {
            if( value.Length > 2 && (value.Substring(0, 2).Equals("0X") || value.Substring(0, 2).Equals("0x")))
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

    public DeclarationList(int lineNumber): base(lineNumber + 1)
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

    public override void markTypes(Type? type = null)
    {
        base.markTypes(type);
        foreach (var child in children)
        {
            if (!variables.TryGetValue(((Variable)child).name, out var variable))
            {
                variables.Add(((Variable)child).name, (Variable)child);
            }
        }
    }

    public override bool validate()
    {
        var result = true;
        var tmpVariables = new Dictionary<String, Variable>();
        foreach(var child in children)
        {
            if (tmpVariables.TryGetValue(((Variable)child).name, out var variable))
            {
                Console.WriteLine($"Line {child.lineNumber}: Variable {variable.name} declared on line {variable.lineNumber}");
                result = false;
            } else
            {
                tmpVariables.Add(((Variable)child).name, (Variable)child);
            }
        }
        return result;
    }

    public override List<Tree> hoistDeclarations()
    {
        return new List<Tree>() { this};
    }
}

public class InstructionList : Tree
{

    public InstructionList(int lineNumber) : base(lineNumber) { }

    public override String genCode()
    {
        string result = "";
        foreach (var child in children)
        {
            result += child.genCode();
        }
        return result;
    }

}

public class Variable : Tree
{
    public string internalIdentifier;
    public string name;
    private static int variablesCount = 0;

    public Variable(Type type, string name, int lineNumber): base(lineNumber)
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

}

public class Relation : Tree
{
    string symbol;
    string comparer;
    string operand;
    Tree originalL, originalR;

    public override void markTypes(Type? type = null)
    {
        originalL.markTypes();
        originalR.markTypes();
        Type generalType = TypeHelper.getMoreGeneralType(originalL.type, originalR.type);
        base.markTypes(generalType);
        if (generalType == Type.Boolean || generalType == Type.Integer)
        {
            comparer = "icmp";
            switch (symbol)
            {
                case "==":
                    operand = "eq";
                    break;
                case "!=":
                    operand = "ne";
                    break;
                case ">":
                    operand = "sgt";
                    break;
                case ">=":
                    operand = "sge";
                    break;
                case "<":
                    operand = "slt";
                    break;
                case "<=":
                    operand = "sle";
                    break;
            }
        }
        else
        {
            comparer = "fcmp";
            switch (symbol)
            {
                case "==":
                    operand = "oeq";
                    break;
                case "!=":
                    operand = "one";
                    break;
                case ">":
                    operand = "ogt";
                    break;
                case ">=":
                    operand = "oge";
                    break;
                case "<":
                    operand = "olt";
                    break;
                case "<=":
                    operand = "ole";
                    break;
            }
        }
    }

    public Relation(Tree lTree, Tree rTree, String symbol, int lineNumber): base(lineNumber) 
    {
        originalL = lTree;
        originalR = rTree;
        this.symbol = symbol;
        this.type = Type.Boolean;
        Type generalType = TypeHelper.getMoreGeneralType(lTree.type, rTree.type);
        children.Add(new Wrapper(generalType, lTree, lineNumber));
        children.Add(new Wrapper(generalType, rTree, lineNumber));
    }

    public override string genCode()
    {
        var result = children[0].genCode();
        result += children[1].genCode();
        result += $"%{resultVariable} = {comparer} {operand} {children[0].type.ToLLVMString()} %{children[0].resultVariable}, %{children[1].resultVariable}\n";
        return result;
    }

    public override bool validate()
    {
        bool result = true;
        if (children[0].type == Type.Boolean && children[1].type == Type.Boolean)
        {
            if (!"==".Equals(symbol) && !"!=".Equals(symbol))
            {
                Console.WriteLine($"Line {lineNumber}: Operator {symbol} can only be used with numeric types");
                result = false;
            }
        }
        result = base.validate() && result;
        return result;
    }
}

public class MathOperator : Tree
{
    string symbol;
    string function;
    Tree originalL, originalR;

    public override void markTypes(Type? type = null)
    {
        originalL.markTypes();
        originalR.markTypes();
        Type generalType = TypeHelper.getMoreGeneralType(originalL.type, originalR.type);
        generalType = TypeHelper.getMoreGeneralType(Type.Integer, generalType);
        this.type = generalType;
        base.markTypes(generalType);
        if (generalType == Type.Double)
        {
            switch (symbol)
            {
                case "+":
                    function = "fadd";
                    break;
                case "-":
                    function = "fsub";
                    break;
                case "*":
                    function = "fmul";
                    break;
                case "/":
                    function = "fdiv";
                    break;
            }
        }
        else
        {
            switch (symbol)
            {
                case "+":
                    function = "add";
                    break;
                case "-":
                    function = "sub";
                    break;
                case "*":
                    function = "mul";
                    break;
                case "/":
                    function = "sdiv";
                    break;
            }
        }
    }

    public MathOperator(Tree lTree, Tree rTree, string symbol, int lineNumber): base(lineNumber)
    {
        originalL = lTree;
        originalR = rTree;
        Type generalType = TypeHelper.getMoreGeneralType(lTree.type, rTree.type);
        generalType = TypeHelper.getMoreGeneralType(Type.Integer, generalType);
        type = generalType;
        children.Add(new Wrapper(generalType, lTree, lineNumber));
        children.Add(new Wrapper(generalType, rTree, lineNumber));
        this.symbol = symbol;
    }

    public override string genCode()
    {
        var result = children[0].genCode();
        result += children[1].genCode();
        result += $"%{resultVariable} = {function} {children[0].type.ToLLVMString()} %{children[0].resultVariable}, %{children[1].resultVariable}\n";
        return result;
    }

}

public class Logical : Tree
{
    string function;
    Tree originalL, originalR;

    public Logical(Tree lTree, Tree rTree, string symbol, int lineNumber): base(lineNumber)
    {
        this.type = Type.Boolean;
        originalL = lTree;
        originalR = rTree;
        children.Add(new Wrapper(Type.Boolean, lTree, lineNumber));
        children.Add(new Wrapper(Type.Boolean, rTree, lineNumber));
        if("&&".Equals(symbol))
        {
            function = "and";
        } else
        {
            function = "or";
        }
    }

    public override void markTypes(Type? type = null)
    {
        originalL.markTypes();
        originalR.markTypes();
        base.markTypes(Type.Boolean);
    }

    public override string genCode()
    {
        var lTreeCode = children[0].genCode();
        var rTreeCode = children[1].genCode();
        if (function.Equals("or"))
        {
            return
                $"br label %logical_start_{uniqueId}\n" +
                $"logical_start_{uniqueId}:\n" +
                lTreeCode + 
                $"br label %logical_prestart_{uniqueId}\n" +
                $"logical_prestart_{uniqueId}:\n" +
                $"br {children[0]}, label %logical_end_{uniqueId}, label %logical_right_{uniqueId}\n" +
                $"logical_right_{uniqueId}:\n" +
                rTreeCode +
                $"br label %logical_preend_{uniqueId}\n" +
                $"logical_preend_{uniqueId}:\n" +
                $"br label %logical_end_{uniqueId}\n" +
                $"logical_end_{uniqueId}:\n" +
                $"%{resultVariable} = phi i1 [true, %logical_prestart_{uniqueId}], [%{children[1].resultVariable}, %logical_preend_{uniqueId}]\n";
        }
        else
        {
            return
                $"br label %logical_start_{uniqueId}\n" +
                $"logical_start_{uniqueId}:\n" +
                lTreeCode +
                $"br label %logical_prestart_{uniqueId}\n" +
                $"logical_prestart_{uniqueId}:\n" +
                $"br {children[0]}, label %logical_right_{uniqueId}, label %logical_end_{uniqueId}\n" +
                $"logical_right_{uniqueId}:\n" +
                rTreeCode +
                $"br label %logical_preend_{uniqueId}\n" +
                $"logical_preend_{uniqueId}:\n" +
                $"br label %logical_end_{uniqueId}\n" +
                $"logical_end_{uniqueId}:\n" +
                $"%{resultVariable} = phi i1 [false, %logical_prestart_{uniqueId}], [%{children[1].resultVariable}, %logical_preend_{uniqueId}]\n";
        }
    }

}

public class Bitwise : Tree
{
    string function;
    Tree originalL, originalR;

    public Bitwise(Tree lTree, Tree rTree, string symbol, int lineNumber): base(lineNumber)
    {
        originalL = lTree;
        originalR = rTree;
        children.Add(new Wrapper(Type.Integer, rTree, lineNumber));
        children.Add(new Wrapper(Type.Integer, lTree, lineNumber));
        type = Type.Integer;
        if ("|".Equals(symbol)) function = "or";
        else function = "and";
    }

    public override void markTypes(Type? type = null)
    {
        originalL.markTypes();
        originalR.markTypes();
        base.markTypes(Type.Integer);
    }

    public override string genCode()
    {
        var result = children[0].genCode();
        result += children[1].genCode();
        result += $"%{resultVariable} = {function} i32 %{children[0].resultVariable}, %{children[1].resultVariable}\n";
        return result;
    }
}

public class Unary : Tree
{
    string symbol;
    Tree originalChild;

    public Unary(Tree child, string symbol, int lineNumber): base(lineNumber)
    {
        this.symbol = symbol;
        originalChild = child;
        children.Add(new Wrapper(Type.Boolean, child, lineNumber));
    }

    public override void markTypes(Type? type = null)
    {
        originalChild.markTypes();
        switch (symbol)
        {
            case "-":
                {
                    children[0].markTypes(TypeHelper.getMoreGeneralType(Type.Integer, originalChild.type));
                    this.type = originalChild.type;
                    break;
                }
            case "~":
                {
                    children[0].markTypes(Type.Integer);
                    this.type = Type.Integer;
                    break;
                }
            case "!":
                {
                    children[0].markTypes(Type.Boolean);
                    this.type = Type.Boolean;
                    break;
                }
        }
    }

    public override string genCode()
    {
        var result = children[0].genCode();
        switch (symbol)
        {
            case "-":
                {
                    if (type == Type.Double)
                    {
                        result += "\n" + $"%{resultVariable} = fneg {children[0]}";
                    }
                    else
                    {
                        result += "\n" + $"%{resultVariable} = sub i32 0, %{children[0].resultVariable}";
                    }
                    break;
                }
            case "~":
                {
                    result += "\n" + $"%{resultVariable} = xor i32 4294967295, %{children[0].resultVariable}";
                    break;
                }
            case "!":
                {
                    result += "\n" + $"%{resultVariable} = select {children[0]}, i1 0, i1 1";
                    break;
                }
        }
        return result + "\n";
    }
}

public class Write : Tree
{
    private bool isHex;
    public Write(Tree child, int lineNumber, bool isHex = false): base(lineNumber)
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
            Console.WriteLine($"Line {lineNumber}: Only integer values can be displayed as hex");
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
    public Read(string identifier, int lineNumber, bool isHex = false) : base(lineNumber)
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
            Console.WriteLine($"Line {lineNumber}: Undeclared identifier: {identifier}");
            return false;
        }
        this.variable = variable;
        if (variable.type != Type.Integer && variable.type != Type.Double)
        {
            Console.WriteLine($"Line {lineNumber}: Can only write to integer and double");
            result = false;
        }
        if (variable.type != Type.Integer && isHex)
        {
            Console.WriteLine($"Line {lineNumber}: Can only write hex to int");
            result = false;
        }
        return result;
    }

}

public class If : Tree
{
    bool hasElse = false;

    public If(Tree condition, Tree body, Tree elseBody, int lineNumber) : base(lineNumber)
    {
        children.Add(condition);
        children.Add(body);
        if(!(elseBody is null))
        {
            hasElse = true;
            children.Add(elseBody);
        }
    }

    public override string genCode()
    {
        var result = children[0].genCode();
        result += $"br {children[0]}, label %if_true_{this.uniqueId}, label %if_false_{this.uniqueId} \n";
        result += $"if_true_{this.uniqueId}:\n";
        result += children[1].genCode();
        result += $"br label %if_end_{this.uniqueId}\n";
        result += $"if_false_{this.uniqueId}:\n";
        if (hasElse)
        {
            result += children[2].genCode();
        }
        result += $"br label %if_end_{this.uniqueId}\n";
        result += $"if_end_{this.uniqueId}: \n";
        return result;
    }

    public override bool validate()
    {
        bool result = base.validate();
        if(children[0].type!=Type.Boolean)
        {
            Console.WriteLine($"Line {lineNumber}: If-condition must be a boolean");
            result = false;
        }
        return result;
    }

}

public class While : Tree
{
    public While(Tree condition, Tree body, int lineNumber) : base(lineNumber)
    {
        children.Add(condition);
        children.Add(body);
    }

    public override string genCode()
    {
        var conditionCode = children[0].genCode();
        var result = $"br label %while_start_{uniqueId}\n";
        result += $"while_start_{uniqueId}:\n";
        result += conditionCode;
        result += $"br {children[0]}, label %while_true_{this.uniqueId}, label %while_end_{this.uniqueId} \n";
        result += $"while_true_{this.uniqueId}:\n";
        result += children[1].genCode();
        result += $"br label %while_start_{this.uniqueId}\n";
        result += $"while_end_{this.uniqueId}: \n";
        return result;
    }

    public override bool validate()
    {
        bool result = base.validate();
        if (children[0].type != Type.Boolean)
        {
            Console.WriteLine($"Line {lineNumber}: While-condition must be a boolean");
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


    public StringLiteral(string value, int lineNumber) :base(lineNumber) {
        stringIdentifier = $"@string_{stringCounter}";
        ++stringCounter;
        this.type = Type.String;
        value = value.Substring(1, value.Length - 2);
        string result = "";
        int escapeSequencesCount = 0;
        for(int i=0; i<value.Length; ++i)
        {
            if (value[i] == '\\')
            {
                switch (value[i + 1])
                {
                    case 'n':
                        result += @"\0A";
                        ++i;
                        escapeSequencesCount++;
                        break;
                    case '"':
                        result += @"\22";
                        ++i;
                        escapeSequencesCount++;
                        break;
                    case '\\':
                        result += @"\5C";
                        escapeSequencesCount++;
                        ++i;
                        break;
                }
                continue;
            }
            else
            {
                result += value[i];
            }
        }
        this.value = result;
        effectiveLength = this.value.Length - 2 * escapeSequencesCount + 1;
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
    Tree originalChild;
    
    public Assign(string identifier, Tree rTree, int lineNumber): base(lineNumber)
    {
        originalChild = rTree;
        this.identifier = identifier;
        this.children.Add(new Wrapper(Type.Boolean, rTree, lineNumber));
    }

    public override string genCode()
    {
        Tree rTree = children[0];
        var variable = getVariable(identifier);
        return rTree.genCode() + "\n" 
            + $"store {rTree}, {variable}\n"
            + $"%{this.resultVariable} = load {this.type.ToLLVMString()}, {variable}\n";
    }

    public override void markTypes(Type? type = null)
    {
        originalChild.markTypes();
        var variable = getVariable(identifier);
        if(!(variable is null))
        {
            this.type = variable.type;
            base.markTypes(this.type);
        }
    }

    public override bool validate()
    {
        Tree rTree = children[0];
        bool result = true;
        var variable = getVariable(identifier);
        if (variable is null)
        {
            Console.WriteLine($"Line {lineNumber}: Undeclared identifier: {identifier}");
            result = false;
        }
        result = rTree.validate() && result;
        return result;
    }
}

// Używane, kiedy jest odwołanie do wartości zmiennej, czyli zawsze oprócz przypisania do zmiennej
public class Identifier : Tree
{
    string identifier;
    public Identifier(string identifier, int lineNumber): base(lineNumber)
    {
        this.identifier = identifier;
    }

    override public string genCode()
    {
        var variable = getVariable(identifier);
        Type type = variable.type;
        string typeString = type.ToLLVMString();
        return $"%{resultVariable} = load {typeString}, {variable}\n";
    }

    public override void markTypes(Type? type = null)
    {
        var variable = getVariable(identifier);
        if (!(variable is null)) this.type = variable.type; 
    }

    public override bool validate()
    {
        var variable = getVariable(identifier);
        if (variable is null)
        {
            Console.WriteLine($"Line {lineNumber}: Undeclared identifier: {identifier}");
            return false;
        }
        this.type = variable.type;
        return true;
    }
}

public class Return : Tree
{
    public Return(int lineNumber) : base(lineNumber) { }

    override public string genCode()
    {
        return "ret i32 0\n";
    }
}


public class Compiler
{

    public static int errors = 0;

    public static List<string> source;
    public static int Main(string[] args)
    {
        string file;
        FileStream source;
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
        sw = new StreamWriter(file + ".ll");
        sw.AutoFlush = true;
        parser.Parse();
        var Program = parser.head;
        if(Program is null)
        {
            Console.WriteLine("Syntax error");
            return 1;
        }
        Program.setParent();
        Program.markTypes();
        if(!Program.validate()) {
            Console.WriteLine("Errors detected, aborting");
            return 1;
        }
        Program.hoistStrings();
        Program.hoistDeclarations();
        var output = Program.genCode();
        sw.Write(output);
        sw.WriteLine();
        sw.Close();
        source.Close();
        return 0;
    }


    private static StreamWriter sw;

}

