using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CorrelatorSharp.WebApi
{
    public class CorrelationIdActionFilter : IActionFilter
    {
        private static readonly string CorrelationIdHttpHeader = Headers.CorrelationId;
        private static readonly string CorrelationParentIdHttpHeader = Headers.CorrelationParentId;
        private static readonly string CorrelationNameHttpHeader = Headers.CorrelationName;

        public bool AllowMultiple => false;

        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            string correlationScopeName = null;
            string correlationId = null;
            string parentCorrelationId = null;

            var headers = actionContext.Request?.Headers;
            if (headers != null)
            {
                if (headers.TryGetValues(CorrelationIdHttpHeader, out var correlationHeaderValue))
                {
                    correlationId = correlationHeaderValue.First();
                }

                if (headers.TryGetValues(CorrelationParentIdHttpHeader, out var correlationParentHeaderValue))
                {
                    parentCorrelationId = correlationParentHeaderValue.First();
                }

                if (headers.TryGetValues(CorrelationNameHttpHeader, out var correlationNameHeaderValue))
                {
                    correlationScopeName = correlationNameHeaderValue.First();
                }
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrWhiteSpace(parentCorrelationId) == false)
            {
                return await InvokeWithParentScope(continuation, correlationScopeName, correlationId, parentCorrelationId);
            }

            return await InvokeWithNewScope(continuation, correlationScopeName, correlationId);
        }

        private static async Task<HttpResponseMessage> InvokeWithNewScope(
            Func<Task<HttpResponseMessage>> continuation,
            string correlationScopeName, string correlationId)
        {
            using (var scope = ActivityScope.Create(correlationScopeName, correlationId))
            {
                var actionResult = await continuation();

                actionResult.Headers.Add(CorrelationIdHttpHeader, scope.Id);

                return actionResult;
            }
        }

        private static async Task<HttpResponseMessage> InvokeWithParentScope(
            Func<Task<HttpResponseMessage>> continuation,
            string correlationScopeName, string correlationId, string parentCorrelationId)
        {
            using (var parent = ActivityScope.Create(null, parentCorrelationId))
            using (var child = ActivityScope.Child(correlationScopeName, correlationId))
            {
                var actionResult = await continuation();

                actionResult.Headers.Add(CorrelationParentIdHttpHeader, parent.Id);
                actionResult.Headers.Add(CorrelationIdHttpHeader, child.Id);

                return actionResult;
            }
        }
    }
}
