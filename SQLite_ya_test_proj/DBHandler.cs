using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using System.IO;

namespace SQLite_ya_test_proj
{
    public class DBHandler
    {
        private static string database_name = "ya_test_db.db3";

        public static void DBCreateStructure()
        {
            using (var database = new SQLiteConnection(database_name, true))
            {
                database.DropTable<Product>();
                database.DropTable<Order>();
                database.CreateTable<Product>();
                database.CreateTable<Order>();

                for (char curr = 'A'; curr <= 'G'; ++curr)
                {
                    database.Insert(new Product { id = curr - 'A' + 1, text = curr.ToString() });
                }
            }
        }

        public static async void DBFillOrders(string path)
        {
            if (!(new FileInfo(path).Exists))
            {
                Console.WriteLine("Loading file not found");
                return;
            }
            var file = new StreamReader(path);
            var dictFields = new Dictionary<string, uint> { { "id", 0 },
                                                            { "dt", 1 },
                                                            { "product_id", 2 },
                                                            { "amount", 3 } };

            var fields = file.ReadLine().Split(' ', '\t').Select(str => dictFields[str]).ToList();
            uint stringNumber = 1;
            var database = new SQLiteAsyncConnection(database_name, true);

            while (!file.EndOfStream)
            {
                var elem = (await file.ReadLineAsync()).Split(' ', '\t');
                if (elem.Count() != 4)
                {
                    Console.WriteLine($"Line {stringNumber}: not enough count of arguments");
                    stringNumber++;
                    continue;
                }

                DateTime dt;
                if (!DateTime.TryParse(elem[fields.IndexOf(1)], out dt))
                {
                    Console.WriteLine($"Line {stringNumber}: can't cast datetime value");
                    stringNumber++;
                    continue;
                }

                int product_id;
                if (!int.TryParse(elem[fields.IndexOf(2)], out product_id))
                {
                    Console.WriteLine($"Line {stringNumber}: can't cast product_id value");
                    stringNumber++;
                    continue;
                }

                double amount;
                if (!double.TryParse(elem[fields.IndexOf(3)].Replace('.', ','), out amount))
                {
                    Console.WriteLine($"Line {stringNumber}: can't cast amount value");
                    stringNumber++;
                    continue;
                }
                await database.InsertAsync(new Order { dt = dt, product_id = product_id, amount = amount });
                stringNumber++;
            }
        }

        public static void DBCountSumQuery()
        {
            using (var database = new SQLiteConnection(database_name, true))
            {
                var query = database.Table<Product>()
                            .GroupJoin(database.Table<Order>(), product => product.id,
                                      order => order.product_id,
                                      (product, orderCollection) =>
                                      new
                                      {
                                          text = product.text,
                                          count = orderCollection
                                          .Where(order => order.dt.Month == DateTime.Today.Month
                                                          && order.dt.Year == DateTime.Today.Year).Count(),
                                          sum = orderCollection
                                          .Where(order => order.dt.Month == DateTime.Today.Month
                                                          && order.dt.Year == DateTime.Today.Year).Sum(order => order.amount)
                                      });

                Console.WriteLine("{0,5} {1,5} {2,8}\n", "Product", "Count", "Sum");
                foreach (var product in query)
                {
                    Console.WriteLine("{0,5} {1,5} {2,8}", product.text, product.count, product.sum);
                }
            }
        }

        public static void DBQueryDiffCurrMonth()
        {
            using (var database = new SQLiteConnection(database_name, true))
            {
                var queryCurrMonth = database.Table<Product>()
                                     .GroupJoin(database.Table<Order>(), product => product.id,
                                                order => order.product_id,
                                                (product, orderCollection) =>
                                                new
                                                {
                                                    text = product.text,
                                                    collect = orderCollection.Select(order => order.dt)
                                                      .Where(date => date.Month == DateTime.Today.Month && date.Year == DateTime.Today.Year)
                                                })
                                     .Where(elem => elem.collect.Count() > 0).Select(elem => elem.text);

                var queryBeforeCurrMonth = database.Table<Product>()
                                     .GroupJoin(database.Table<Order>(), product => product.id,
                                                order => order.product_id,
                                                (product, orderCollection) =>
                                                new
                                                {
                                                    text = product.text,
                                                    collect = orderCollection.Select(order => order.dt)
                                                      .Where(date => date.Month < DateTime.Today.Month && date.Year <= DateTime.Today.Year)
                                                })
                                     .Where(elem => elem.collect.Count() > 0).Select(elem => elem.text);

                var queryLastMonth = database.Table<Product>()
                                     .GroupJoin(database.Table<Order>(), product => product.id,
                                                order => order.product_id,
                                                (product, orderCollection) =>
                                                new
                                                {
                                                    text = product.text,
                                                    collect = orderCollection.Select(order => order.dt)
                                                      .Where(date => date.Month == DateTime.Today.Month - 1 && date.Year == DateTime.Today.Year)
                                                })
                                      .Where(elem => elem.collect.Count() > 0).Select(elem => elem.text);

                var queryDiffCurrBefore = queryCurrMonth.Except(queryBeforeCurrMonth);
                Console.WriteLine("Product, ordered firstly");
                foreach (var product in queryDiffCurrBefore)
                {
                    Console.WriteLine(product);
                }

                var queryDiffCurrLast = queryCurrMonth.Except(queryLastMonth);
                Console.WriteLine("Product, ordered in current, but not in last month");
                foreach (var product in queryDiffCurrLast)
                {
                    Console.WriteLine(product);
                }

                var queryDiffLastCurr = queryLastMonth.Except(queryCurrMonth);
                Console.WriteLine("Product, ordered in last, but not in current month");
                foreach (var product in queryDiffLastCurr)
                {
                    Console.WriteLine(product);
                }
            }
        }

        public static void DBQueryMonthStatistics()
        {
            using (var database = new SQLiteConnection(database_name, true))
            {
                var query = database.Table<Order>()
                            .GroupBy(elem => elem.Age, elem => elem, (age, elem) => new
                            {
                                age = age,
                                productMax = elem.GroupBy(curr => curr.product_id, curr => curr.amount,
                                                           (id, sum) => new
                                                           { id = id,
                                                             sum = sum.Sum(),
                                                             count = sum.Count() })
                                                   .Aggregate((max, next) => max.count > next.count ? max : next),
                                total = elem.Count()
                            });

                Console.WriteLine("{0,5} {1,5} {2,8} {3,8}\n", "Age", "Product", "Sum", "Portion");
                foreach (var ageStat in query)
                {
                    var productName = database.Table<Product>().Where(prod => prod.id == ageStat.productMax.id).First().text;
                    var percent = (int)((double)ageStat.productMax.count / ageStat.total * 100);
                    Console.WriteLine("{0,15} {1,5} {2,8} {3,8}", ageStat.age, productName, ageStat.productMax.sum, percent);
                }
            }
        }

    }
}
