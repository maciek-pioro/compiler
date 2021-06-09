
// Uwaga: W wywołaniu generatora gppg należy użyć opcji /gplex

%namespace GardensPoint

%union
{
  public string value;
  public int lineNumber;
  public Tree tree;
  public List<string> stringList;
  public List<int> linesList;
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

block_inside      : declaration_list instruction_list {$$.tree = new Block($1.tree, $2.tree, $1.lineNumber); } ;

declaration_list  : declaration declaration_list {
                      $$.tree = $2.tree;
                      $$.tree.children.AddRange($1.tree.children);
                  }
                  | {
                      $$.tree = new DeclarationList(0);
                  }
                  ;

declaration       : type identifier_list Semicolon  {
                      $$.tree = new DeclarationList($1.lineNumber);
                      Type type = $1.type;
                      for(int i=0; i<$2.stringList.Count; ++i){
                        $$.tree.children.Add(new Variable(type, $2.stringList[i], $2.linesList[i]));
                      }
                  };

identifier_list   : Ident {
                      $$.stringList = new List<string>();
                      $$.linesList = new List<int>();
                      $$.stringList.Add($1.value);
                      $$.linesList.Add($1.lineNumber);
                  }
                  | Ident Comma identifier_list {
                      $3.stringList.Add($1.value);
                      $$.linesList.Add($1.lineNumber);
                      $$.stringList = $3.stringList;
                      $$.linesList = $3.linesList;
                  }
                  ;


instruction       : assignment Semicolon { $$.tree = $1.tree; }
                  | If OpenPar assignment ClosePar instruction {$$.tree = new If($3.tree, $5.tree, null, $1.lineNumber);}
                  | If OpenPar assignment ClosePar instruction Else instruction {$$.tree = new If($3.tree, $5.tree, $7.tree, $1.lineNumber);}
                  | While OpenPar assignment ClosePar instruction {$$.tree = new While($3.tree, $5.tree, $1.lineNumber);}
                  | OpenBra block_inside CloseBra {$$.tree = $2.tree;}
                  | Read Ident Comma Hex Semicolon { $$.tree = new Read($2.value, $1.lineNumber, true); }
                  | Read Ident Semicolon { $$.tree = new Read($2.value, $1.lineNumber); }
                  | Write assignment Comma Hex Semicolon { $$.tree = new Write($2.tree, $1.lineNumber, true); }
                  | Write assignment Semicolon { $$.tree = new Write($2.tree, $1.lineNumber); }
                  | Write String Semicolon { $$.tree = new Write(new StringLiteral($2.value, $1.lineNumber), $1.lineNumber); }
                  | Return Semicolon { $$.tree = new Return($1.lineNumber); }
                  ;


instruction_list  : instruction_list instruction {
                      $$.tree = $1.tree;
                      $$.tree.children.Add($2.tree);
                  }
                  | {
                      $$.tree = new InstructionList(0);
                  }
                  ;




type              : Int { $$.type = Type.Integer; }
                  | Double { $$.type = Type.Double; }
                  | Bool { $$.type = Type.Boolean; };

constant          : RealNumber { $$.tree = new Literal(Type.Double, $1.value, $1.lineNumber); }
                  | IntNumber { $$.tree = new Literal(Type.Integer, $1.value, $1.lineNumber); }
                  | True { $$.tree = new Literal(Type.Boolean, "true", $1.lineNumber); }
                  | False { $$.tree = new Literal(Type.Boolean, "false", $1.lineNumber); }
                  ;

leaf              : constant
                  | Ident { $$.tree = new Identifier($1.value, $1.lineNumber); }
                  | OpenPar assignment ClosePar {$$.tree = $2.tree; }
                  ;
unary             : Minus unary {$$.tree = new Unary($2.tree, "-", $1.lineNumber);}
                  | BitNot unary {$$.tree = new Unary($2.tree, "~", $1.lineNumber);}
                  | LogicalNot unary {$$.tree = new Unary($2.tree, "!", $1.lineNumber); }
                  | OpenPar Int ClosePar unary {$$.tree = new Wrapper(Type.Integer, $4.tree, $1.lineNumber, true); }
                  | OpenPar Double ClosePar unary {$$.tree = new Wrapper(Type.Double, $4.tree, $1.lineNumber, true); }
                  | leaf;
bitwise           : bitwise BitOr unary {$$.tree = new Bitwise($1.tree, $3.tree, "|", $1.lineNumber); }
                  | bitwise BitAnd unary {$$.tree = new Bitwise($1.tree, $3.tree, "&", $1.lineNumber); }
                  | unary;
multiplicative    : multiplicative Multiplies bitwise {$$.tree = new MathOperator($1.tree, $3.tree, "*", $1.lineNumber); }
                  | multiplicative Divides bitwise {$$.tree = new MathOperator($1.tree, $3.tree, "/", $1.lineNumber); }
                  | bitwise;
additive          : additive Plus multiplicative {$$.tree = new MathOperator($1.tree, $3.tree, "+", $1.lineNumber); }
                  | additive Minus multiplicative {$$.tree = new MathOperator($1.tree, $3.tree, "-", $1.lineNumber); }
                  | multiplicative;
relation          : relation Equals additive {$$.tree = new Relation($1.tree, $3.tree, "==", $1.lineNumber); }
                  | relation NotEquals additive {$$.tree = new Relation($1.tree, $3.tree, "!=", $1.lineNumber); }
                  | relation GT additive {$$.tree = new Relation($1.tree, $3.tree, ">", $1.lineNumber); }
                  | relation GEQ additive {$$.tree = new Relation($1.tree, $3.tree, ">=", $1.lineNumber); }
                  | relation LT additive {$$.tree = new Relation($1.tree, $3.tree, "<", $1.lineNumber); }
                  | relation LEQ additive {$$.tree = new Relation($1.tree, $3.tree, "<=", $1.lineNumber); }
                  | additive;
logical           : logical LogicalOr relation {$$.tree = new Logical($1.tree, $3.tree, "||", $1.lineNumber); }
                  | logical LogicalAnd relation {$$.tree = new Logical($1.tree, $3.tree, "&&", $1.lineNumber); }
                  | relation;
assignment        : Ident Assign assignment {
                    $$.tree = new Assign($1.value, $3.tree, $1.lineNumber);
                  }
                  | logical;



%%
public Parser(Scanner scanner) : base(scanner) { }
public Tree head;

