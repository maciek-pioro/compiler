
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

%%


start             : Program 
                    OpenBra 
                    block_inside
                    CloseBra 
                    EOF {
                      head = new Program($3.tree);
                    }
                  ;

block_inside      : declaration_list instruction_list {$$.tree = new Block($1.tree, $2.tree); } ;

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


instruction       : assignment Semicolon { $$.tree = $1.tree; }
                  | If OpenPar assignment ClosePar instruction {$$.tree = new If($3.tree, $5.tree, null);}
                  | If OpenPar assignment ClosePar instruction Else instruction {$$.tree = new If($3.tree, $5.tree, $7.tree);}
                  | While OpenPar assignment ClosePar instruction {$$.tree = new While($3.tree, $5.tree);}
                  | OpenBra block_inside CloseBra {$$.tree = $2.tree;}
                  | Read Ident Comma Hex Semicolon { $$.tree = new Read($2.value, true); }
                  | Read Ident Semicolon { $$.tree = new Read($2.value); }
                  | Write assignment Comma Hex Semicolon { $$.tree = new Write($2.tree, true); }
                  | Write assignment Semicolon { $$.tree = new Write($2.tree); }
                  | Write String Semicolon { $$.tree = new Write(new StringLiteral($2.value)); }
                  | Return Semicolon { $$.tree = new Return(); }
                  ;


instruction_list  : instruction_list instruction {
                      $$.tree = $1.tree;
                      $$.tree.children.Add($2.tree);
                  }
                  | {
                      $$.tree = new InstructionList();
                  }
                  ;




type              : Int { $$.type = Type.Integer; }
                  | Double { $$.type = Type.Double; }
                  | Bool { $$.type = Type.Boolean; };

constant          : RealNumber { $$.tree = new Literal(Type.Double, $1.value); }
                  | IntNumber { $$.tree = new Literal(Type.Integer, $1.value); }
                  | True { $$.tree = new Literal(Type.Boolean, "true"); }
                  | False { $$.tree = new Literal(Type.Boolean, "false"); }
                  ;

leaf              : constant
                  | Ident { $$.tree = new Identifier($1.value); }
                  | OpenPar assignment ClosePar {$$.tree = $2 .tree}
                  ;
unary             : Minus unary 
                  | BitNot unary
                  | LogicalNot unary
                  | OpenPar Int ClosePar unary
                  | OpenPar Double ClosePar unary
                  | leaf;
bitwise           : bitwise BitOr unary {$$.tree = new Bitwise($1.tree, $3.tree, "&"); }
                  | bitwise BitAnd unary {$$.tree = new Bitwise($1.tree, $3.tree, "|"); }
                  | unary;
multiplicative    : multiplicative Multiplies bitwise {$$.tree = new MathOperator($1.tree, $3.tree, "*"); }
                  | multiplicative Divides bitwise {$$.tree = new MathOperator($1.tree, $3.tree, "/"); }
                  | bitwise;
additive          : additive Plus multiplicative {$$.tree = new MathOperator($1.tree, $3.tree, "+"); }
                  | additive Minus multiplicative {$$.tree = new MathOperator($1.tree, $3.tree, "-"); }
                  | multiplicative;
relation          : relation Equals additive {$$.tree = new Relation($1.tree, $3.tree, "=="); }
                  | relation NotEquals additive {$$.tree = new Relation($1.tree, $3.tree, "!="); }
                  | relation GT additive {$$.tree = new Relation($1.tree, $3.tree, ">"); }
                  | relation GEQ additive {$$.tree = new Relation($1.tree, $3.tree, ">="); }
                  | relation LT additive {$$.tree = new Relation($1.tree, $3.tree, "<"); }
                  | relation LEQ additive {$$.tree = new Relation($1.tree, $3.tree, "<="); }
                  | additive;
logical           : logical LogicalOr relation
                  | logical LogicalAnd relation
                  | relation;
assignment        : Ident Assign assignment {
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
