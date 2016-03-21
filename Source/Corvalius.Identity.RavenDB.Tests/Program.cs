using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corvalius.Identity.RavenDB.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dp = new DefaultPocoTest();
            dp.CanIncludeUserLoginsTest();
        }
    }
}
