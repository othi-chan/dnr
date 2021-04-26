using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dnr.Service.Auth.Abstractions;
using Dnr.Web.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Dnr.Web.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [Route("")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IEnumerable<AccountGet>))]
        public ActionResult GetAll()
        {
            var accounts = _authService.GetAll();

            var accountsGet = new List<AccountGet>();
            foreach (var account in accounts)
                accountsGet.Add(new AccountGet
                {
                    Id = account.Id,
                    Login = account.Login,
                    Password = account.Password,
                });

            return Ok(accountsGet);
        }

        [HttpGet]
        [Route("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(AccountGet))]
        public ActionResult Get(int id)
        {
            var account = _authService.Get(id);
            return Ok(new AccountGet
            {
                Id = account.Id,
                Login = account.Login,
                Password = account.Password,
            });
        }

        [HttpGet]
        [Route("login/{login}/{password}")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(long))]
        public ActionResult Login(string login, string password)
        {
            var account = _authService.Login(login, password);
            return Ok(account.Id);
        }

        [HttpPost]
        [Route("")]
        [SwaggerResponse(StatusCodes.Status201Created, "Created person id.", typeof(long))]
        public ActionResult Register([FromBody] AccountPost data)
        {
            var account = _authService.Register(data.Login!, data.Password!);
            return Ok(account.Id);
        }
    }
}