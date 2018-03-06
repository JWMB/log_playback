using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SqlPlayer
{
    public class SQLiteExecuter : SqlExecuter
    {
        protected override DbProviderFactory GetFactory()
        {
            return Microsoft.Data.Sqlite.SqliteFactory.Instance;
        }
    }
}
