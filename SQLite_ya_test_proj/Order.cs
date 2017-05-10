using System;
using System.Linq;
using SQLite;


namespace SQLite_ya_test_proj
{
    public class Order
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int      id { get; set; }

        [NotNull]
        public DateTime dt { get; set; }

        [NotNull]
        public int      product_id { get; set; }

        [NotNull]
        public double   amount { get; set; }

        [Ignore]
        public string Age
        {
            get
            {
                return dt.GetDateTimeFormats('Y').First();
            }
        }
    }
}
