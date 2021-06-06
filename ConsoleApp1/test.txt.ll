@i32HexFormat = constant [5 x i8] c"0X%X\00"
@i32Format = constant [3 x i8] c"%d\00"
@doubleFormat = constant [3 x i8] c"%f\00"
@stringFormat = constant [3 x i8] c"%s\00"
@trueString = constant [5 x i8] c"True\00"
@falseString = constant [6 x i8] c"False\00"
@readInt = constant[3 x i8] c"%d\00"
@readIntHex = constant[3 x i8] c"%X\00"
@readDouble = constant[4 x i8] c"%lf\00"

@string_0 = constant[5 x i8] c"a==b\00"
@string_1 = constant[5 x i8] c"a!=b\00"
@string_2 = constant[5 x i8] c"a==c\00"
@string_3 = constant[5 x i8] c"c!=d\00"
@string_4 = constant[5 x i8] c"a==d\00"

 declare i32 @printf(i8*, ...)
declare i32 @scanf(i8 *, ...) 
define i32 @main(){
%l_double = alloca double
            %r_double = alloca double
            %result_double = alloca double
            %l_i32 = alloca i32
            %r_i32 = alloca i32
            %result_i32 = alloca i32
            %l_i1 = alloca i1
            %r_i1 = alloca i1
            %result_i1 = alloca i1
%variable_2 = alloca double
%variable_3 = alloca double
%variable_0 = alloca i32
%variable_1 = alloca i32






store i32 1, i32* %result_i32
%tmp_8 = load i32, i32* %result_i32

store i32 %tmp_8, i32* %variable_1
store i32 3, i32* %result_i32
%tmp_11 = load i32, i32* %result_i32

store i32 %tmp_11, i32* %variable_0
store double 1.0, double* %result_double
%tmp_14 = load double, double* %result_double

store double %tmp_14, double* %variable_3
store double 3.0, double* %result_double
%tmp_17 = load double, double* %result_double

store double %tmp_17, double* %variable_2
%tmp_20 = load i32, i32* %variable_1
%tmp_21 = load i32, i32* %variable_0
%tmp_22 = icmp eq i32 %tmp_20, %tmp_21
br i1 %tmp_22, label %if_true_30, label %if_false_30 
if_true_30:
call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), i8* bitcast([5 x i8]* @string_0 to i8*))

br label %if_end_30
if_false_30:
br label %if_end_30
if_end_30: 
%tmp_31 = load i32, i32* %variable_1
%tmp_32 = load i32, i32* %variable_0
%tmp_33 = icmp ne i32 %tmp_31, %tmp_32
br i1 %tmp_33, label %if_true_41, label %if_false_41 
if_true_41:
call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), i8* bitcast([5 x i8]* @string_1 to i8*))

br label %if_end_41
if_false_41:
br label %if_end_41
if_end_41: 
%tmp_42 = load i32, i32* %variable_1

%tmp_45 = sitofp i32 %tmp_42 to double
%tmp_43 = load double, double* %variable_3
%tmp_44 = fcmp oeq double %tmp_45, %tmp_43
br i1 %tmp_44, label %if_true_52, label %if_false_52 
if_true_52:
call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), i8* bitcast([5 x i8]* @string_2 to i8*))

br label %if_end_52
if_false_52:
br label %if_end_52
if_end_52: 
%tmp_53 = load double, double* %variable_3
%tmp_54 = load double, double* %variable_2
%tmp_55 = fcmp one double %tmp_53, %tmp_54
br i1 %tmp_55, label %if_true_63, label %if_false_63 
if_true_63:
call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), i8* bitcast([5 x i8]* @string_3 to i8*))

br label %if_end_63
if_false_63:
br label %if_end_63
if_end_63: 
%tmp_64 = load i32, i32* %variable_1

%tmp_67 = sitofp i32 %tmp_64 to double
%tmp_65 = load double, double* %variable_2
%tmp_66 = fcmp oeq double %tmp_67, %tmp_65
br i1 %tmp_66, label %if_true_74, label %if_false_74 
if_true_74:
call i32(i8*, ...) @printf(i8 * bitcast([3 x i8] * @stringFormat to i8 *), i8* bitcast([5 x i8]* @string_4 to i8*))

br label %if_end_74
if_false_74:
br label %if_end_74
if_end_74: 

ret i32 0
}
