using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Collections.Generic;
using System.Reflection;

namespace WebFramework.Swagger
{
    public class UnauthorizedResponsesOperationFilter : IOperationFilter
    {
        private readonly bool _include;
        private readonly string _schemeName;

        public UnauthorizedResponsesOperationFilter(bool includeUnauthorizedAndForbiddenResponses, string schemeName = "Bearer")
        {
            _include = includeUnauthorizedAndForbiddenResponses;
            _schemeName = schemeName;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // اگر MethodInfo نبود، بی‌خیال
            if (!context.ApiDescription.TryGetMethodInfo(out var methodInfo))
                return;

            // AllowAnonymous روی اکشن یا کنترلر
            var allowAnonymous =
                methodInfo.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                methodInfo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;

            if (allowAnonymous) return;

            // Authorize روی اکشن یا کنترلر
            var hasAuthorize =
                methodInfo.IsDefined(typeof(AuthorizeAttribute), true) ||
                methodInfo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), true) == true;

            if (!hasAuthorize) return;

            if (_include)
            {
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(_schemeName), new List<string>() }
            });

        }
    }

    public class AllowAnonymousClearSecurityOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.TryGetMethodInfo(out MethodInfo methodInfo))
                return;

            bool isAnonymous =
                methodInfo.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                methodInfo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;

            if (isAnonymous)
            {
                // این باعث می‌شه global security برای این operation override بشه
                operation.Security = new List<OpenApiSecurityRequirement>();
            }
        }
    }
    public class JwtAuthorizeOperationFilter : IOperationFilter
    {
        private readonly bool _include401And403;
        private readonly string _schemeName;

        public JwtAuthorizeOperationFilter(bool include401And403, string schemeName = "Bearer")
        {
            _include401And403 = include401And403;
            _schemeName = schemeName;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.TryGetMethodInfo(out MethodInfo methodInfo))
                return;

            // اگر Anonymous باشد: عمداً هیچ Security برای این Operation نذار (قفل باز)
            bool isAnonymous =
                methodInfo.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                methodInfo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;

            if (isAnonymous)
            {
                // اگر قبلاً چیزی ست شده بود، پاکش کن که swagger.json برای این اکشن security نده
                operation.Security = new List<OpenApiSecurityRequirement>();
                return;
            }

            // اگر Authorize باشد: SecurityRequirement اضافه کن (قفل بسته + ارسال توکن)
            bool isAuthorized =
                methodInfo.IsDefined(typeof(AuthorizeAttribute), true) ||
                methodInfo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), true) == true;

            if (!isAuthorized)
                return;

            if (_include401And403)
            {
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(_schemeName), new List<string>() }
            });
        }
    }
}




