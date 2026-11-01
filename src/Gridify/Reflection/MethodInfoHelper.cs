using System;
using System.Linq;
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
/// </summary>
public static class MethodInfoHelper
{
   public static MethodInfo GetToLowerMethod()
   {
      return typeof(string).GetMethod("ToLower", [])!;
   }

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
   {
      return typeof(string).GetMethod("EndsWith", [typeof(string)])!;
   }

   public static MethodInfo GetStartWithMethod()
   {
      return typeof(string).GetMethod("StartsWith", [typeof(string)])!;
   }

   public static MethodInfo GetStringContainsMethod()
   {
      return typeof(string).GetMethod("Contains", [typeof(string)])!;
   }

#if NET
   [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Enumerable))]
#endif
   public static MethodInfo GetContainsMethod(Type tp)
   {
      return CreateGenericMethod(typeof(Enumerable).GetMethods().First(x => x.Name == "Contains"), tp);
   }

   public static MethodInfo GetIsNullOrEmptyMethod()
   {
      return typeof(string).GetMethod("IsNullOrEmpty", [typeof(string)])!;
   }

   public static MethodInfo GetCompareMethodWithStringComparison()
   {
      return typeof(string).GetMethod("Compare", [typeof(string), typeof(string), typeof(StringComparison)])!;
   }

   public static MethodInfo GetCompareMethod()
   {
      return typeof(string).GetMethod("Compare", [typeof(string), typeof(string)])!;
   }

   public static MethodInfo GetToStringMethod()
   {
      return typeof(object).GetMethod("ToString")!;
   }

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
