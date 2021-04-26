using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dnr.Service.Auth.Models;

namespace Dnr.Service.Auth.Abstractions
{
    public interface IAuthService
    {
        Account Register(string login, string password);

        Account Login(string login, string password);

        Account Get(int id);

        IEnumerable<Account> GetAll();
    }
}