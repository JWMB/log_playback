using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SqlPlayer
{
    public class MSSqlExecuter : SqlExecuter
    {
        protected override DbProviderFactory GetFactory()
        {
            return System.Data.SqlClient.SqlClientFactory.Instance;
        }
    }
}
