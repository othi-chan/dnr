using Dnr.Service.Auth.Models.Abstractions;

namespace Dnr.Service.Auth.Models
{
    public class Account : IEntity<long>
    {
        public long Id { get; set; }

        public string? Login { get; set; }

        public string? Password { get; set; }

        public int VictoriesTotal { get; set; }

        public int DefeatsTotal { get; set; }

        public int GamesTotal => VictoriesTotal + DefeatsTotal;
    }
}