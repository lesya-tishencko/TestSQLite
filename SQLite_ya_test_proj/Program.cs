using System;

namespace SQLite_ya_test_proj
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Don't set name of source file");
                return;
            }

            string path = args[0];
            Console.WriteLine(path);
            DBHandler.DBCreateStructure();
            DBHandler.DBFillOrders(path);

            int commandNum = -1;
            while (commandNum != 0)
            {
                Console.WriteLine("Put query number(1,2,3): ");
                if (!int.TryParse(Console.ReadLine(), out commandNum))
                {
                    commandNum = -1;
                    continue;
                }
                switch (commandNum)
                {
                    case 1:
                        DBHandler.DBCountSumQuery();
                        break;
                    case 2:
                        DBHandler.DBQueryDiffCurrMonth();
                        break;
                    case 3:
                        DBHandler.DBQueryMonthStatistics();
                        break;
                    default:
                        Console.WriteLine("Invalid number of query");
                        break;
                }
            }
        }
    }
}
