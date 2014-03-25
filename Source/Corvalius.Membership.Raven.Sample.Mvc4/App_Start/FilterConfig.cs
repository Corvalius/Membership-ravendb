using System.Web;
using System.Web.Mvc;

namespace Corvalius.Membership.Raven.Sample.Mvc4
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}