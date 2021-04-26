using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dnr.Service.Auth.Abstractions;
using Dnr.Service.Auth.Models;

namespace Dnr.Service.Auth
{
    public class AuthService : IAuthService
    {
        private List<Account> Accounts { get; set; }

        public AuthService()
        {
            Accounts = new List<Account>
            {
                new Account
                {
                    Id = 0,
                    Login = "admin",
                    Password = "admin",
                    VictoriesTotal = 0,
                    DefeatsTotal = 0,
                }
            };
        }

        public Account Get(int id)
        {
            return Accounts.ElementAt(id);
        }

        public Account Login(string login, string password)
        {
            return Accounts.Single(_ => _.Login == login && _.Password == password);
        }

        public Account Register(string login, string password)
        {
            Accounts.Add(new Account
            {
                Id = Accounts.Count,
                Login = login,
                Password = password,
                VictoriesTotal = 0,
                DefeatsTotal = 0,
            });
            return Accounts.Last();
        }

        public IEnumerable<Account> GetAll()
        {
            return Accounts;
        }
    }
}