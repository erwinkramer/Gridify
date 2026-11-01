using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Gridify.Reflection;

/// <summary>
/// The single place in Gridify that builds <see cref="LambdaExpression"/> trees.
/// <para>
/// Every tree built here is handed to a LINQ query provider (Entity Framework,
/// LinqToObjects, Elasticsearch, ...) which interprets it; Gridify never calls
/// <c>Compile()</c> on them. Building a tree emits no code - only compiling it to a
/// delegate would - which is why <c>Expression.Lambda</c> is annotated
/// <c>RequiresDynamicCode</c>. The IL3050 suppression inside this class is therefore
/// the only AOT/trimming suppression Gridify needs for query-provider lambda trees.
/// </para>
/// </summary>
internal static class LambdaFactory
{
#pragma warning disable IL3050
   public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
      => Expression.Lambda(body, parameters);

   public static Expression<TResult> Lambda<TResult>(Expression body, params ParameterExpression[] parameters)
      => Expression.Lambda<TResult>(body, parameters);

   public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[] parameters)
      => Expression.Lambda(delegateType, body, parameters);

   /// <summary>
   /// Convenience overload for parameter lists already collected in a collection
   /// (e.g. <see cref="LambdaExpression.Parameters"/>).
   /// </summary>
   public static LambdaExpression Lambda(Expression body, IEnumerable<ParameterExpression> parameters)
      => Lambda(body, parameters.ToArray());
#pragma warning restore IL3050
}
