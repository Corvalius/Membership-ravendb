using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corvalius.Membership.Raven
{
    internal class RolesIndex : AbstractIndexCreationTask<RoleEntity>
    {
        public RolesIndex()
        {
            Map = roles => from doc in roles
                           select new { doc.Id, doc.Name };

            StoreAllFields(FieldStorage.No);
        }
    }

    internal class RolesByUsersIndex : AbstractIndexCreationTask<UserRoleRef>
    {
        public RolesByUsersIndex()
        {
            Map = roles => from doc in roles
                           select new { doc.Username, doc.Role };

            StoreAllFields(FieldStorage.No);
        }
    }
}
