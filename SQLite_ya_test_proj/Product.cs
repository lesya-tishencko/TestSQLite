using SQLite;

namespace SQLite_ya_test_proj
{
    public class Product
    {
        [PrimaryKey, Unique]
        public int    id { get; set; }

        [MaxLength(30), NotNull]
        public string text { get; set; }
    }
}
