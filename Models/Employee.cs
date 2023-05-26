using Microsoft.EntityFrameworkCore;

namespace WebApiPractice.Models
{
    [PrimaryKey(nameof(name))]
    public class Employee
    {
        public string? name { get; set; }
        public string? email { get; set; }
        public string? tel { get; set; }
        public string? joined { get; set; }
    }
}
