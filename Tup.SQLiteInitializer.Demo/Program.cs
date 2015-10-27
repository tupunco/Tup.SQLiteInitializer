using System;

namespace Tup.SQLiteInitializer.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SQLiteDatabaseInitializer.TryInitializer(new DefaultSQLiteInitializer());

            Console.ReadKey();
        }
    }
}