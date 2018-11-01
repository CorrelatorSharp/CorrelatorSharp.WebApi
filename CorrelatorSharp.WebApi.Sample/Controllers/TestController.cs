using System.Web.Http;

namespace CorrelatorSharp.WebApi.Sample.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Test() 
            => Json(ActivityScope.Current);
    }
}
