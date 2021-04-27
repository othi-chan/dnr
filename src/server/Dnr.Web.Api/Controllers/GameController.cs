using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dnr.Service.Auth.Abstractions;
using Dnr.Service.Game.Abstractions;
using Dnr.Service.Game.Models;
using Dnr.Web.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Dnr.Web.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GameController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IGameService _gameService;

        public GameController(IAuthService authService, IGameService gameService)
        {
            _authService = authService;
            _gameService = gameService;
        }

        [HttpGet]
        [Route("")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(IEnumerable<SessionGet>))]
        public ActionResult GetAll()
        {
            var sessions = _gameService.Sessions.Values.ToList();

            var sessionsGet = new List<SessionGet>();
            foreach (var session in sessions)
                sessionsGet.Add(new SessionGet
                {
                    Id = session!.Id,
                    Players = session.Players.Values,
                    PlayersCapacity = session.PlayersCapacity,
                    GameStarted = session.GameStarted,
                    Map = (GameMap)session.Map!,
                    Winner = session.Winner,
                });

            return Ok(sessionsGet);
        }

        [HttpGet]
        [Route("{id:Guid}")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(SessionGet))]
        public ActionResult Get(Guid id)
        {
            var succeed = _gameService.Sessions.TryGetValue(id, out var session);
            return succeed
                ? Ok(new SessionGet
                {
                    Id = session!.Id,
                    Players = session.Players.Values,
                    PlayersCapacity = session.PlayersCapacity,
                    GameStarted = session.GameStarted,
                    Map = (GameMap)session.Map!,
                    Winner = session.Winner,
                })
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost]
        [Route("")]
        [SwaggerResponse(StatusCodes.Status201Created, "Created session id.", typeof(Guid))]
        public ActionResult CreateSession([FromBody] SessionPost data)
        {
            var account = _authService.Get(data.AccauntId);
            var (succeed, session) = _gameService.CreateSession(account.Id, account.Login!);
            return succeed ? Ok(session!.Id) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut]
        [Route("attach/{sessionId:Guid}/{accountId:int}")]
        [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(void))]
        public ActionResult AttachSession(Guid sessionId, int accountId)
        {
            var account = _authService.Get(accountId);
            var (succeed, _) = _gameService.AttachSession(accountId, account.Login!, sessionId);
            return succeed ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete]
        [Route("detach/{sessionId:Guid}/{accountId:int}")]
        [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(void))]
        public ActionResult DetachSession(Guid sessionId, int accountId)
        {
            var account = _authService.Get(accountId);
            var (succeed, _) = _gameService.DetachSession(accountId, sessionId);
            return succeed ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut]
        [Route("start/{sessionId:Guid}")]
        [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(void))]
        public ActionResult StartGame(Guid sessionId)
        {
            var (succeed, _) = _gameService.StartGame(sessionId);
            return succeed ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut]
        [Route("levelup/{sessionId:Guid}/{village}")]
        [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(void))]
        public ActionResult LevelUp(Guid sessionId, string village)
        {
            var (succeed, _) = _gameService.VillageLevelUp(sessionId, village);
            return succeed ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut]
        [Route("army/{sessionId:Guid}")]
        [SwaggerResponse(StatusCodes.Status204NoContent, type: typeof(void))]
        public ActionResult SendArmy(Guid sessionId, [FromBody] SendArmyPut data)
        {
            var (succeed, _) = _gameService.SendArmy(sessionId, data.SourceVillage!, data.TargetVillage!, data.ArmyCount);
            return succeed ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}