using System.Web.Http;

namespace CorrelatorSharp.WebApi.Sample.Controllers
{
    public class TestController : ApiController
    {

        public IHttpActionResult Test()
        {
            return Json(ActivityScope.Current.Id);
        }
           
    }
}
