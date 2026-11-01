using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif

namespace Gridify.Reflection;

/// <summary>
/// The <see cref="DynamicDependency"/> attributes on the <c>MakeGenericMethod</c> call sites
/// keep the generic method bodies of <c>Enumerable.Any</c>, <c>Enumerable.Contains</c> and
/// <c>Enumerable.Select</c> in trimmed and native AOT builds, so that <c>MakeGenericMethod</c>
/// can (re)instantiate them at runtime.
///
/// The non-generic getters resolve their <see cref="MethodInfo"/> from static expression trees
/// (instead of string-based reflection), which is allowed in trimmed and native AOT builds.
/// </summary>
public static class MethodInfoHelper
{
   /// <summary>
   /// Returns the method of the first method call in <paramref name="call"/>.
   /// The call is never invoked; it only exists so the compiler resolves the
   /// <see cref="MethodInfo"/> statically (AOT-safe, unlike <c>Type.GetMethod(string)</c>).
   /// </summary>
   private static MethodInfo MethodOf<TArg, TResult>(Expression<Func<TArg, TResult>> call)
      => ((MethodCallExpression)call.Body).Method;

   public static MethodInfo GetToLowerMethod()
      => MethodOf<string, string>(s => s.ToLower());

#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Enumerable))]
#endif
   public static MethodInfo GetAnyMethod(Type type)
   {
      return CreateGenericMethod(
         typeof(Enumerable).GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Length == 2),
         type);
   }

   public static MethodInfo GetEndsWithMethod()
      => MethodOf<string, bool>(s => s.EndsWith(string.Empty));

   public static MethodInfo GetStartWithMethod()
      => MethodOf<string, bool>(s => s.StartsWith(string.Empty));

   public static MethodInfo GetStringContainsMethod()
      => MethodOf<string, bool>(s => s.Contains(string.Empty));

#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Enumerable))]
#endif
   public static MethodInfo GetContainsMethod(Type tp)
   {
      return CreateGenericMethod(typeof(Enumerable).GetMethods().First(x => x.Name == "Contains"), tp);
   }

   public static MethodInfo GetIsNullOrEmptyMethod()
      => MethodOf<string, bool>(s => string.IsNullOrEmpty(s));

   public static MethodInfo GetCompareMethodWithStringComparison()
      => MethodOf<string, int>(s => string.Compare(s, string.Empty, StringComparison.Ordinal));

   public static MethodInfo GetCompareMethod()
      => MethodOf<string, int>(s => string.Compare(s, string.Empty));

   public static MethodInfo GetToStringMethod()
      => MethodOf<object, string>(o => o.ToString());

#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Enumerable))]
#endif
   public static MethodInfo GetSelectMethod(this Type type)
   {
      return CreateGenericMethod(typeof(Enumerable).GetMethods().First(m => m.Name == "Select"), type, type);
   }

#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Enumerable))]
#endif
   public static MethodInfo GetSelectMethod(Type sourceType, Type resultType)
   {
      return CreateGenericMethod(
         typeof(Enumerable).GetMethods().First(m => m.Name == "Select" && m.GetParameters().Length == 2),
         sourceType,
         resultType);
   }

   /// <summary>
   /// The single place in Gridify that instantiates a generic BCL method at runtime.
   /// <c>MakeGenericMethod</c> is <c>RequiresDynamicCode</c>, so this method carries the
   /// only IL3050/IL2060 suppression Gridify needs for it. The generic method bodies
   /// that get instantiated stay in trimmed and native AOT builds through the
   /// <see cref="DynamicDependency"/> attributes on the getters above.
   /// </summary>
#pragma warning disable IL3050, IL2060
   public static MethodInfo CreateGenericMethod(MethodInfo genericMethod, params Type[] typeArguments)
      => genericMethod.MakeGenericMethod(typeArguments);
#pragma warning restore IL3050, IL2060

   /// <summary>
   /// The single place in Gridify that resolves and instantiates a public static
   /// <see cref="Queryable"/> method (e.g. "OrderBy") at runtime. The resolved method
   /// is only used to build expression trees that the query provider interprets; it is
   /// never delegate-compiled here.
   /// </summary>
#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Queryable))]
#endif
   public static MethodInfo GetQueryableMethod(string methodName, params Type[] typeArguments)
   {
      // Only the OrderBy/ThenBy family is ever resolved here; the other Queryable members
      // flagged by enumerating the type (e.g. AsQueryable) are never touched, and
      // DynamicDependency above keeps the sorted methods in trimmed and AOT builds.
#pragma warning disable IL2026
      var genericMethod = typeof(Queryable).GetMethods().Cast<MethodInfo>()
         .First(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeArguments.Length);
#pragma warning restore IL2026
      return CreateGenericMethod(genericMethod, typeArguments);
   }
}
