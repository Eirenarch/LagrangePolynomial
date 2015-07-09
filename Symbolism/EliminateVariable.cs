﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Symbolism.AlgebraicExpand;
using Symbolism.IsolateVariable;
using Symbolism.SimplifyEquation;

namespace Symbolism.EliminateVariable
{
    public static class Extensions
    {
        public static MathObject CheckVariableEqLs(this List<Equation> eqs, Symbol sym)
        {
            // (a == 10, a == 0)   ->   10 == 0   ->   false

            if (eqs.EliminateVariableEqLs(sym) == false) return false;

            // (1/a != 0  &&  a != 0)   ->   a != 0

            if (eqs.Any(eq => eq.Operator == Equation.Operators.NotEqual && eq.a == sym && eq.b == 0)
                &&
                eqs.Any(eq => eq.Operator == Equation.Operators.NotEqual && eq.a == 1 / sym && eq.b == 0))
                return eqs
                    .Where(eq => (eq.Operator == Equation.Operators.NotEqual && eq.a == 1 / sym && eq.b == 0) == false)
                    .ToList()
                    .CheckVariableEqLs(sym);

            return new And() { args = eqs.Select(eq => eq as MathObject).ToList() };
        }

        public static MathObject CheckVariable(this MathObject expr, Symbol sym)
        {
            // 1 / x == 0
            // 1 / x^2 == 0

            if (expr is Equation &&
                (expr as Equation).Operator == Equation.Operators.Equal &&
                (expr as Equation).b == 0 &&
                (expr as Equation).a.Has(sym) &&
                (expr as Equation).SimplifyEquation() is Equation &&
                ((expr as Equation).SimplifyEquation() as Equation).a is Power &&
                (((expr as Equation).SimplifyEquation() as Equation).a as Power).exp is Integer &&
                ((((expr as Equation).SimplifyEquation() as Equation).a as Power).exp as Integer).val < 0)
                return false;

            if (expr is And)
            {
                var result = (expr as And).Map(elt => elt.CheckVariable(sym));

                if (result is And)
                {
                    var eqs = (expr as And).args.Select(elt => elt as Equation).ToList();

                    return eqs.CheckVariableEqLs(sym);
                }

                return result;
            }


            if (expr is Or &&
                (expr as Or).args.All(elt => elt is And))
                return (expr as Or).Map(elt => elt.CheckVariable(sym));

            return expr;
        }



        // EliminateVarAnd
        // EliminateVarOr
        // EliminateVarLs
        // EliminateVar

        // EliminateVars

        public static MathObject EliminateVariableEqLs(this List<Equation> eqs, Symbol sym)
        {
            if (eqs.Any(elt =>
                    elt.Operator == Equation.Operators.Equal &&
                    elt.Has(sym) &&
                    elt.AlgebraicExpand().Has(sym) &&
                    elt.IsolateVariableEq(sym).Has(obj => obj is Equation && (obj as Equation).a == sym && (obj as Equation).b.FreeOf(sym))
                    ) == false)
                return new And() { args = eqs.Select(elt => elt as MathObject).ToList() };

            var eq = eqs.First(elt =>
                elt.Operator == Equation.Operators.Equal &&
                elt.Has(sym) &&
                elt.AlgebraicExpand().Has(sym) &&
                elt.IsolateVariableEq(sym).Has(obj => obj is Equation && (obj as Equation).a == sym && (obj as Equation).b.FreeOf(sym)));

            var rest = eqs.Except(new List<Equation>() { eq });

            var result = eq.IsolateVariableEq(sym);

            // sym was not isolated

            if (result is Equation && 
                ((result as Equation).a != sym || (result as Equation).b.Has(sym)))
                return new And() { args = eqs.Select(elt => elt as MathObject).ToList() };
            
            if (result is Equation)
            {
                var eq_sym = result as Equation;

                return new And() { args = rest.Select(elt => elt.Substitute(sym, eq_sym.b)).ToList() }.Simplify();

                // return new And() { args = rest.Select(rest_eq => rest_eq.SubstituteEq(eq_sym)).ToList() };

                // rest.Map(rest_eq => rest_eq.Substitute(eq_sym)
            }

            // Or(
            //     And(eq0, eq1, eq2, ...)
            //     And(eq3, eq4, eq5, ...)
            // )

            if (result is Or && (result as Or).args.All(elt => elt is And))
            {
                (result as Or).args.ForEach(elt => (elt as And).args.AddRange(rest));

                return new Or() { args = (result as Or).args.Select(elt => EliminateVariable(elt, sym)).ToList() };
            }

            if (result is Or)
            {
                var or = new Or();

                foreach (Equation eq_sym in (result as Or).args)
                    or.args.Add(new And() { args = rest.Select(rest_eq => rest_eq.Substitute(sym, eq_sym.b)).ToList() }.Simplify());

                return or;

                // (result as Or).Map(eq_sym => new And() { args = rest.Select(rest_eq => rest_eq.SubstituteEq(eq_sym)).ToList() });

                // (result as Or).Map(eq_sym => rest.Map(rest_eq => rest_eq.Substitute(eq_sym))
            }

            throw new Exception();
        }

        public static MathObject EliminateVariable(this MathObject expr, Symbol sym)
        {
            if (expr is And)
            {
                var eqs = (expr as And).args.Select(elt => elt as Equation);

                return EliminateVariableEqLs(eqs.ToList(), sym);
            }

            if (expr is Or)
            {
                return new Or() { args = (expr as Or).args.Select(and_expr => and_expr.EliminateVariable(sym)).ToList() };

                // expr.Map(and_expr => and_expr.EliminateVar(sym))
            }

            throw new Exception();
        }

        public static MathObject EliminateVariables(this MathObject expr, params Symbol[] syms)
        { return syms.Aggregate(expr, (result, sym) => result.EliminateVariable(sym)); }
    }
}
