
%using QUT.Gppg;
%namespace GardensPoint

IntDec              (0|[1-9][0-9]*)
IntHex              (0x|0X)[0-9a-fA-F]+
RealNumber          (0|[1-9][0-9]*)\.[0-9]+
Ident               [a-zA-Z][a-zA-Z0-9]*
Comment             \/\/.*\n
String              \"[^"]*\"
Whitespace          [ \t\n\f\r]

%%
{Comment}           {  }
"program"           { yylval.lineNumber=yyline; return (int)Tokens.Program; }
"if"                { yylval.lineNumber=yyline; return (int)Tokens.If; }
"else"              { yylval.lineNumber=yyline; return (int)Tokens.Else; }
"while"             { yylval.lineNumber=yyline; return (int)Tokens.While; }
"read"              { yylval.lineNumber=yyline; return (int)Tokens.Read; }
"write"             { yylval.lineNumber=yyline; return (int)Tokens.Write; }
"return"            { yylval.lineNumber=yyline; return (int)Tokens.Return; }

"int"               { yylval.lineNumber=yyline; return (int)Tokens.Int; }
"double"            { yylval.lineNumber=yyline; return (int)Tokens.Double; }
"bool"              { yylval.lineNumber=yyline; return (int)Tokens.Bool; }

"true"              { yylval.lineNumber=yyline; return (int)Tokens.True; }
"false"             { yylval.lineNumber=yyline; return (int)Tokens.False; }
"hex"               { yylval.lineNumber=yyline; return (int)Tokens.Hex; }

{Ident}             { yylval.lineNumber=yyline; yylval.value=yytext; return (int)Tokens.Ident; }
{IntDec}            { yylval.lineNumber=yyline; yylval.value=yytext; return (int)Tokens.IntNumber; }
{IntHex}            { yylval.lineNumber=yyline; yylval.value=yytext; return (int)Tokens.IntNumber; }
{RealNumber}        { yylval.lineNumber=yyline; yylval.value=yytext; return (int)Tokens.RealNumber; }
{String}            { yylval.lineNumber=yyline; yylval.value=yytext; return (int)Tokens.String; }

"=="                { yylval.lineNumber=yyline; return (int)Tokens.Equals; }
"!="                { yylval.lineNumber=yyline; return (int)Tokens.NotEquals; }
">="                { yylval.lineNumber=yyline; return (int)Tokens.GEQ; }
"<="                { yylval.lineNumber=yyline; return (int)Tokens.LEQ; }
"||"                { yylval.lineNumber=yyline; return (int)Tokens.LogicalOr; }
"&&"                { yylval.lineNumber=yyline; return (int)Tokens.LogicalAnd; }

"="                 { yylval.lineNumber=yyline; return (int)Tokens.Assign; }
">"                 { yylval.lineNumber=yyline; return (int)Tokens.GT; }
"<"                 { yylval.lineNumber=yyline; return (int)Tokens.LT; }
","                 { yylval.lineNumber=yyline; return (int)Tokens.Comma; }
"!"                 { yylval.lineNumber=yyline; return (int)Tokens.LogicalNot; }
"&"                 { yylval.lineNumber=yyline; return (int)Tokens.BitAnd; }
"|"                 { yylval.lineNumber=yyline; return (int)Tokens.BitOr; }
"~"                 { yylval.lineNumber=yyline; return (int)Tokens.BitNot; }
"+"                 { yylval.lineNumber=yyline; return (int)Tokens.Plus; }
"-"                 { yylval.lineNumber=yyline; return (int)Tokens.Minus; }
"*"                 { yylval.lineNumber=yyline; return (int)Tokens.Multiplies; }
"/"                 { yylval.lineNumber=yyline; return (int)Tokens.Divides; }

"("                 { yylval.lineNumber=yyline; return (int)Tokens.OpenPar; }
")"                 { yylval.lineNumber=yyline; return (int)Tokens.ClosePar; }
"{"                 { yylval.lineNumber=yyline; return (int)Tokens.OpenBra; }
"}"                 { yylval.lineNumber=yyline; return (int)Tokens.CloseBra; }
";"                 { yylval.lineNumber=yyline; return (int)Tokens.Semicolon; }


{Whitespace}          { }

.               { return (int)Tokens.Error; }
