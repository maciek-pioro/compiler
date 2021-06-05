
// Uwaga: W wywołaniu generatora gppg należy użyć opcji /gplex

%namespace GardensPoint

%union
{
  public string value;
  public int lineNumber;
  public Tree tree;
  public List<String> stringList;
  public Type type;
  public List<Tree> treeList;
}
%token IntNumber RealNumber Plus String Ident Program If Else While Read Write Return Int Double Bool True False Hex Equals NotEquals GEQ LEQ LogicalOr LogicalAnd Assign GT LT Comma LogicalNot BitAnd BitOr BitNot Minus Multiplies Divides OpenPar ClosePar OpenBra CloseBra Semicolon Eof Error Endl

/* %type <type> declaration instruction identifier_list type instruction_list */
/* %type <val>  constant expression */
/* %type <Tree> declaration instruction type instruction_list constant expression */
 /* exp term factor */

%%


start             : Program 
                    OpenBra 
                    declaration_list 
                    instruction_list
                    CloseBra 
                    EOF 
                    {
                      head = new Program($3.tree, $4.tree);
                      $3.tree.parent = head;
                      $4.tree.parent = head;
                    }
                  ;



declaration_list  : declaration declaration_list {
  $$.tree = $2.tree;
  $$.tree.children.AddRange($1.tree.children);
}
                  | {
                    $$.tree = new DeclarationList();
                  }
                  ;

declaration       : type identifier_list Semicolon  {
  $$.tree = new DeclarationList();
  Type type = $1.type;
  foreach(string name in $2.stringList) {
    $$.tree.children.Add(new Variable(type, name));
  }
};

identifier_list   : Ident {
  $$.stringList = new List<string>();
  $$.stringList.Add($1.value);
}
                  | Ident Comma identifier_list {
                    $3.stringList.Add($1.value);
                    $$.stringList = $3.stringList;
                  }
                  ;


instruction       : Read {
                    // Compiler.EmitCode($"The expression equals {$$}");
                  } Ident Semicolon
                  | assignment Semicolon {
                    $$.tree = $1.tree;
                  }
                  | If {Compiler.EmitCode("If-Else");} OpenPar assignment ClosePar instruction Else instruction
                  | While OpenPar assignment ClosePar instruction
                  | Read Ident Comma Hex Semicolon
                  | Write assignment Comma Hex Semicolon
                  | Write assignment Semicolon {
                    $$.tree = new Write($2.tree);
                  }
                  | Return Semicolon
                  ;
    /* call i32 (i8*, ...) @printf(i8* bitcast ([19 x i8]* @prompt to i8*)) */


instruction_list  : instruction_list instruction {
                    $$.tree = $1.tree;
                    $$.tree.children.Add($2.tree);
}
                  | {
                    $$.tree = new InstructionList();
                  }
                  ;




type              : Int {
  $$.type = Type.Integer;
}
                  | Double {
  $$.type = Type.Double;
}
                  | Bool {
  $$.type = Type.Boolean;
};

/* operator          : Plus {
                      $$ = $2;
                    }; */

constant          : RealNumber {
                    $$.tree = new Literal(Type.Double, $1.value);
}
                  | IntNumber {
                    $$.tree = new Literal(Type.Integer , $1.value);
}
                  | True 
                  | False   
                  | String 
                  ;

leaf              : constant
                  | Ident {
                    $$.tree = new Identifier($1.value);
                  }
                  | OpenPar assignment ClosePar
                  ;
unary             : Minus unary 
                  | BitNot unary
                  | LogicalNot unary
                  | OpenPar Int ClosePar unary
                  | OpenPar Double ClosePar unary
                  | leaf;
bitwise           : bitwise BitOr unary
                  | bitwise BitAnd unary
                  | unary;
multiplicative    : multiplicative Multiplies bitwise
                  | multiplicative Divides bitwise
                  | bitwise;
additive          : additive Plus multiplicative
                  | additive Minus multiplicative
                  | multiplicative;
relation          : relation Equals additive
                  | relation NotEquals additive
                  | relation GT additive
                  | relation GEQ additive
                  | relation LT additive
                  | relation LEQ additive
                  | additive;
logical           : logical LogicalOr relation
                  | logical LogicalAnd relation
                  | relation;
assignment        : Ident Assign assignment {
  Console.WriteLine("adding assignment");
  $$.tree = new Assign($1.value, $3.tree);
}
                  | logical;



%%
public Parser(Scanner scanner) : base(scanner) { }
public Tree head;
/* private char BinaryOpGenCode(Tokens t, char type1, char type2)
    {
    char type = ( type1=='i' && type2=='i' ) ? 'i' : 'r' ;
    if ( type1!=type )
        {
        Compiler.EmitCode("sldloc temp");
        }tloc temp");
        Compiler.EmitCode("conv.r8");
        Compiler.EmitCode("
    if ( type2!=type )
        Compiler.EmitCode("conv.r8");
    switch ( t )
        {
        case Tokens.Plus:
            Compiler.EmitCode("add");
            break;
        case Tokens.Minus:
            Compiler.EmitCode("sub");
            break;
        case Tokens.Multiplies:
            Compiler.EmitCode("mul");
            break;
        case Tokens.Divides:
            Compiler.EmitCode("div");
            break;
        default:
            Console.WriteLine($"  line {lineno,3}:  internal gencode error");
            ++Compiler.errors;
            break;
        }
    return type;
    } */
