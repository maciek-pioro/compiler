
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



declaration       : type identifier_list Semicolon  {
  $$.tree = new DeclarationList();
  Type type = $1.type;
  foreach(string name in $2.stringList) {
    $$.tree.children.Add(new Variable(type));
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
                  | expression {Compiler.EmitCode("Expression");} Semicolon
                  | If {Compiler.EmitCode("If-Else");} OpenPar expression ClosePar instruction Else instruction
                  /* | If {Compiler.EmitCode("If");} OpenPar expression ClosePar instruction  */
                  | While OpenPar expression ClosePar instruction
                  | Read Ident Comma Hex Semicolon
                  | Write {
                    // Compiler.EmitCode($"The expression equals {$1}");
                  } constant Semicolon {
                    // Compiler.EmitCode("X");
                  }
                  | Write expression Comma Hex Semicolon
                  | Write expression Semicolon {
                    $$.tree = new Write($2.tree);
                  }
                  | Return Semicolon
                  ;
    /* call i32 (i8*, ...) @printf(i8* bitcast ([19 x i8]* @prompt to i8*)) */


instruction_list  : instruction_list instruction
                  | {
                    $$.tree = new DeclarationList();
                  }
                  ;

declaration_list  : declaration declaration_list {
  $$.tree = $2.tree;
  $$.tree.children.Add($1.tree);
}
                  | {
                    $$.tree = new DeclarationList();
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
  // $$.tree = new Constant(Type.Double, $1.value);
}
                  | IntNumber 
                  | True
                  | False
                  | String 
                  ;

expression        : expression Plus expression {
                    $$ = $$;
                  }
                  | Ident Assign expression {
                  }
                  | Ident
                  | constant {
                    $$.tree = $1.tree;
                  }
                  ;

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
