using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tup.SQLiteInitializer.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            SQLiteDatabaseInitializer.TryInitializer(new DefaultSQLiteInitializer());

            Console.ReadKey();
        }
    }
}
