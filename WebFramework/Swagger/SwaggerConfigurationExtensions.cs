using Asp.Versioning;

using Common;
using Common.Utilities;

using ElmahCore.Mvc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WebFramework.Swagger
{
    public static class SwaggerConfigurationExtensions
    {
        public static void AddSwagger(this IServiceCollection services)
        {
            Assert.NotNull(services, nameof(services));

            //More info : https://github.com/mattfrear/Swashbuckle.AspNetCore.Filters

            #region AddSwaggerExamples
            //Add services to use Example Filters in swagger
            //If you want to use the Request and Response example filters (and have called options.ExampleFilters() above), then you MUST also call
            //This method to register all ExamplesProvider classes form the assembly
            //services.AddSwaggerExamplesFromAssemblyOf<PersonRequestExample>();

            //We call this method for by reflection with the Startup type of entry assmebly (MyApi assembly)
            var mainAssembly = Assembly.GetEntryAssembly(); // => MyApi project assembly
            var mainType = mainAssembly.GetExportedTypes()[0];

            const string methodName = nameof(Swashbuckle.AspNetCore.Filters.ServiceCollectionExtensions.AddSwaggerExamplesFromAssemblyOf);
            //MethodInfo method = typeof(Swashbuckle.AspNetCore.Filters.ServiceCollectionExtensions).GetMethod(methodName);
            MethodInfo method = typeof(Swashbuckle.AspNetCore.Filters.ServiceCollectionExtensions).GetRuntimeMethods().FirstOrDefault(x => x.Name == methodName && x.IsGenericMethod);
            MethodInfo generic = method.MakeGenericMethod(mainType);
            generic.Invoke(null, new[] { services });
            #endregion

            //Add services and configuration to use swagger
            services.AddSwaggerGen(options =>
            {
                var xmlDocPath = Path.Combine(AppContext.BaseDirectory, "MyApi.xml");
                //show controller XML comments like summary
                options.IncludeXmlComments(xmlDocPath, true);

                options.EnableAnnotations();

                #region DescribeAllEnumsAsStrings
                //This method was Deprecated. 
                //options.DescribeAllEnumsAsStrings();

                //You can specify an enum to convert to/from string, uisng :
                //[JsonConverter(typeof(StringEnumConverter))]
                //public virtual MyEnums MyEnum { get; set; }

                //Or can apply the StringEnumConverter to all enums globaly, using :
                //Used in ServiceCollectionExtensions.AddMinimalMvc
                //.AddNewtonsoftJson(option => option.SerializerSettings.Converters.Add(new StringEnumConverter()));
                #endregion

                //options.DescribeAllParametersInCamelCase();
                //options.DescribeStringEnumsInCamelCase()
                //options.UseReferencedDefinitionsForEnums()
                //options.IgnoreObsoleteActions();
                //options.IgnoreObsoleteProperties();

                options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "Patient API" });
                options.SwaggerDoc("v2", new OpenApiInfo { Version = "v2", Title = "Doctor API" });
                options.SwaggerDoc("v3", new OpenApiInfo { Version = "v3", Title = "Labratory API" });

                #region Filters
                //Enable to use [SwaggerRequestExample] & [SwaggerResponseExample]
                options.ExampleFilters();

                //It doesn't work anymore in recent versions because of replacing Swashbuckle.AspNetCore.Examples with Swashbuckle.AspNetCore.Filters
                //Adds an Upload button to endpoints which have [AddSwaggerFileUploadButton]
                //options.OperationFilter<AddFileParamTypesOperationFilter>();

                //Set summary of action if not already set
                options.OperationFilter<ApplySummariesOperationFilter>();

                #region Add UnAuthorized to Response
                //Add 401 response and security requirements (Lock icon) to actions that need authorization
                //options.OperationFilter<UnauthorizedResponsesOperationFilter>(true, "OAuth2");
                //options.OperationFilter<UnauthorizedResponsesOperationFilter>(true, "Bearer");


                #endregion

                #region Add Jwt Authentication
                //Add Lockout icon on top of swagger ui page to authenticate
                #region Old way
                //options.AddSecurityDefinition("Bearer", new ApiKeyScheme
                //{
                //    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                //    Name = "Authorization",
                //    In = "header"
                //});
                //options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                //{
                //    {"Bearer", new string[] { }}
                //});
                #endregion

                //options.AddSecurityRequirement(new OpenApiSecurityRequirement
                //{
                //    {
                //        new OpenApiSecurityScheme
                //        {
                //            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" }
                //        },
                //        Array.Empty<string>() //new[] { "readAccess", "writeAccess" }
                //    }
                //});

                //OAuth2Scheme
                //options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                //{
                //    Scheme = "bearer",
                //    In = ParameterLocation.Header,
                //    Type = SecuritySchemeType.Http,
                //    Flows = new OpenApiOAuthFlows
                //    {
                //        Password = new OpenApiOAuthFlow
                //        {
                //            TokenUrl = new Uri("/api/v1/auth/FreeToken", UriKind.Relative),
                //            //AuthorizationUrl = new Uri("/api/v1/users/Token", UriKind.Relative)
                //            //Scopes = new Dictionary<string, string>
                //            //{
                //            //    { "readAccess", "Access read operations" },
                //            //    { "writeAccess", "Access write operations" }
                //            //}
                //        }
                //    }
                //});
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                //                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                //{
                //    { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
                //});
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
{
    { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
});

                //options.OperationFilter<JwtAuthorizeOperationFilter>(true, "Bearer");
                options.OperationFilter<AllowAnonymousClearSecurityOperationFilter>();

                #endregion

                #region Versioning
                // Remove version parameter from all Operations
                options.OperationFilter<RemoveVersionParameters>();

                //set version "api/v{version}/[controller]" from current swagger doc verion
                options.DocumentFilter<SetVersionInPaths>();

                //Seperate and categorize end-points by doc version
                options.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;

                    var versions = methodInfo.DeclaringType
                        .GetCustomAttributes<ApiVersionAttribute>(true)
                        .SelectMany(attr => attr.Versions);

                    return versions.Any(v => $"v{v}" == docName);
                });
                #endregion

                //If use FluentValidation then must be use this package to show validation in swagger (MicroElements.Swashbuckle.FluentValidation)
                //options.AddFluentValidationRules();
                #endregion
            });
        }

        public static IApplicationBuilder UseElmahCore(this IApplicationBuilder app, SiteSettings siteSettings)
        {
            Assert.NotNull(app, nameof(app));
            Assert.NotNull(siteSettings, nameof(siteSettings));

            app.UseWhen(context => context.Request.Path.StartsWithSegments(siteSettings.ElmahPath, StringComparison.OrdinalIgnoreCase), appBuilder =>
            {
                appBuilder.Use((ctx, next) =>
                {
                    ctx.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                    return next();
                });
            });
            app.UseElmah();

            return app;
        }

        public static IApplicationBuilder UseSwaggerAndUI(this IApplicationBuilder app)
        {
            Assert.NotNull(app, nameof(app));

            //More info : https://github.com/domaindrivendev/Swashbuckle.AspNetCore

            //Swagger middleware for generate "Open API Documentation" in swagger.json
            app.UseSwagger(/*options =>
            {
                options.RouteTemplate = "api-docs/{documentName}/swagger.json";
            }*/);

            //Swagger middleware for generate UI from swagger.json
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient API");
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Doctor API");
                options.SwaggerEndpoint("/swagger/v3/swagger.json", "Labratory API");

                #region Customizing
                //// Display
                //options.DefaultModelExpandDepth(2);
                //options.DefaultModelRendering(ModelRendering.Model);
                //options.DefaultModelsExpandDepth(-1);
                //options.DisplayOperationId();
                //options.DisplayRequestDuration();
                options.DocExpansion(DocExpansion.None);
                //options.EnableDeepLinking();
                //options.EnableFilter();
                //options.MaxDisplayedTags(5);
                //options.ShowExtensions();

                //// Network
                //options.EnableValidator();
                //options.SupportedSubmitMethods(SubmitMethod.Get);

                //// Other
                //options.DocumentTitle = "CustomUIConfig";
                //options.InjectStylesheet("/ext/custom-stylesheet.css");
                //options.InjectJavascript("/ext/custom-javascript.js");
                //options.RoutePrefix = "api-docs";
                #endregion
            });

            //ReDoc UI middleware. ReDoc UI is an alternative to swagger-ui
            app.UseReDoc(options =>
            {
                options.SpecUrl("/swagger/v1/swagger.json");
                options.SpecUrl("/swagger/v2/swagger.json");
                options.SpecUrl("/swagger/v3/swagger.json");

                #region Customizing
                //By default, the ReDoc UI will be exposed at "/api-docs"
                //options.RoutePrefix = "docs";
                //options.DocumentTitle = "My API Docs";

                options.EnableUntrustedSpec();
                options.ScrollYOffset(10);
                options.HideHostname();
                options.HideDownloadButton();
                options.ExpandResponses("200,201");
                options.RequiredPropsFirst();
                options.NoAutoAuth();
                options.PathInMiddlePanel();
                options.HideLoading();
                options.NativeScrollbars();
                options.DisableSearch();
                options.OnlyRequiredInSamples();
                options.SortPropsAlphabetically();
                #endregion
            });

            return app;
        }
    }
}
