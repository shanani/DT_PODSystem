using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace DT_PODSystem.Areas.Security.Helpers
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync<TModel>(string viewName, TModel model);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ViewRenderService(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> RenderToStringAsync<TModel>(string viewName, TModel model)
        {
            // Normalize the view path for areas
            var viewEngineResult = _viewEngine.GetView(null, viewName, false);

            if (!viewEngineResult.Success)
            {
                throw new InvalidOperationException($"Could not find view {viewName}");
            }

            var view = viewEngineResult.View;

            using (var output = new StringWriter())
            {
                // Create a new ActionContext
                var httpContext = _httpContextAccessor.HttpContext ??
                    new DefaultHttpContext { RequestServices = _serviceProvider };

                var actionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor()
                );

                // Create ViewDataDictionary
                var viewData = new ViewDataDictionary<TModel>(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary())
                {
                    Model = model
                };

                // Create TempDataDictionary
                var tempData = new TempDataDictionary(
                    actionContext.HttpContext,
                    _tempDataProvider
                );

                // Render the view
                await view.RenderAsync(new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    tempData,
                    output,
                    new HtmlHelperOptions()
                ));

                return output.ToString();
            }
        }
    }
}