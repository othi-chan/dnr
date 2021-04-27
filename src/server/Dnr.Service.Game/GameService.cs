using Dnr.Service.Game.Abstractions;
using Dnr.Service.Game.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dnr.Service.Game
{
    public class GameService : IGameService
    {
        public ConcurrentDictionary<Guid, Session> Sessions { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private Timer GeneralTimer { get; }

        private ConcurrentDictionary<Guid, Timer> GameTimers { get; }

        private ConcurrentDictionary<Guid, Timer> WinTimers { get; }

        private LinkedList<(Army army, Timer timer, Guid sessionId)> BattleTimers { get; }

        public GameService()
        {
            Sessions = new ConcurrentDictionary<Guid, Session>();
            GeneralTimer = new Timer(
                new TimerCallback(UpdateSessionStates),
                state: null,
                dueTime: 1000,
                period: 1000);
            GameTimers = new ConcurrentDictionary<Guid, Timer>();
            WinTimers = new ConcurrentDictionary<Guid, Timer>();
            BattleTimers = new LinkedList<(Army army, Timer timer, Guid sessionId)>();
        }

        public (bool succeed, Session? session) AttachSession(long accauntId, string playerLogin, Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            if (session.Players.Count >= session.PlayersCapacity)
                return (succeed: false, session);

            var player = new Player(accauntId, playerLogin);
            var succeedPlayerAdd = session.Players.TryAdd(accauntId, player);

            return (succeed: succeedPlayerAdd, session);
        }

        public (bool succeed, Session? session) CreateSession(long accauntId, string creatorLogin)
        {
            var sessionKey = Guid.NewGuid();
            while (Sessions.ContainsKey(sessionKey))
                sessionKey = Guid.NewGuid();

            var newSession = new Session(sessionKey);
            var succeedSessionAdd = Sessions.TryAdd(sessionKey, newSession);
            if (!succeedSessionAdd)
                return (succeed: false, session: null);

            return AttachSession(accauntId, creatorLogin, sessionKey);
        }

        public (bool succeed, Session? session) DetachSession(long accauntId, Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var succeedPlayerGet = session.Players.TryGetValue(accauntId, out var player);
            if (!succeedPlayerGet || player == null)
                return (succeed: false, session);

            var succeedPlayerRemove = session.Players.TryRemove(player.Id, out _);
            if (session.Players.Any())
                return (succeed: succeedPlayerRemove, session);

            var succeedSessionRemove = Sessions.TryRemove(sessionId, out _);
            return (succeed: succeedSessionRemove, session: null);
        }

        public (bool succeed, Session? session) StartGame(Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            if (session.Players.Count != session.PlayersCapacity)
                return (succeed: false, session);

            var gameTimer = new Timer(
                new TimerCallback(GameOver),
                state: sessionId,
                dueTime: (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
                period: Timeout.Infinite);
            GameTimers.TryAdd(sessionId, gameTimer);

            session.Map = new GameMap(session.Players.Values);
            return (succeed: true, session);
        }

        private void UpdateSessionStates(object? state)
        {
            foreach (var session in Sessions.Values)
            {
                if (!session.GameStarted)
                    continue;

                foreach (var player in session.Players.Values)
                {
                    foreach (var location in session.Map!.Locations.Where(_ => _.Owner != null && _.Owner.Id == player.Id))
                    {
                        if (location is Village village)
                            player.Influence += village.InfluenceGrowth;
                        if (location is Castle castle)
                            player.Influence += castle.InfluenceGrowth;
                    }
                }

                foreach (var location in session.Map!.Locations)
                {
                    if (location is Village village)
                        village.ArmyCount += village.ArmyGrowth;
                    if (location is Castle castle)
                        castle.ArmyCount += castle.ArmyGrowth;
                }
            }
        }

        public (bool succeed, Session? session) VillageLevelUp(Guid sessionId, string villageName)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var village = (Village)session.Map!.Locations.Single(_ => _.Name == villageName);
            if (village.Owner == null || village.Owner.Influence < village.LevelUpCost)
                return (succeed: false, session);

            village.Level++;
            return (succeed: true, session);
        }

        public (bool succeed, Session? session) SendArmy(
            Guid sessionId,
            string sourceName,
            string targetName,
            int armyCount)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var sourceVillage = (Village)session.Map!.Locations.Single(_ => _.Name == sourceName);
            var road = sourceVillage.Roads.Single(_ => _.Ends.Item1.Name == targetName || _.Ends.Item2.Name == targetName);
            var targetVillage = road.Ends.Item1.Name == targetName ? road.Ends.Item1 : road.Ends.Item2;

            if (armyCount > sourceVillage.ArmyCount)
                return (succeed: false, session);
            sourceVillage.ArmyCount -= armyCount;

            var army = new Army(owner: sourceVillage.Owner!, sourceVillage, targetVillage, road, armyCount);
            road.Armies.Add(army);

            // Находим ближайшую армию, с которой столкнемся
            var elapsedBattleTime = (army.FinishTime - army.StartTime).TotalSeconds;
            var elapsedDistance = road.Length;
            foreach (var enemyArmy in road.Armies.Where(_ => _.Owner.Id != sourceVillage.Owner!.Id))
            {
                elapsedBattleTime = Math.Min(elapsedBattleTime, (enemyArmy.FinishTime - army.StartTime).TotalSeconds);
                elapsedDistance = Math.Min(elapsedDistance, elapsedBattleTime / (army.FinishTime - army.StartTime).TotalSeconds * road.Length);
            }
            var battleTimer = new Timer(
                new TimerCallback(SimulateBattle),
                state: army,
                dueTime: elapsedDistance == road.Length
                            ? (int)elapsedBattleTime * 1000
                            : (int)(elapsedDistance / ((elapsedDistance / elapsedBattleTime) + (army.SpeedModifier * road.SpeedModifier * 1)) * 1000),
                period: Timeout.Infinite);
            BattleTimers.AddLast((army, battleTimer, sessionId));

            return (succeed: true, session);
        }

        private void SimulateBattle(object? state)
        {
            var army = (Army)state!;
            var currentTime = DateTime.UtcNow;

            // Если дошли до деревни
            if (Math.Abs((army.FinishTime - currentTime).TotalMilliseconds) < 1000)
            {
                // Если деревня принадлежит владельцу армии
                if (army.Target.Owner != null && army.Target.Owner.Name == army.Owner.Name)
                {
                    // Добавляем армию к гарнизону
                    if (army.Target is Village village)
                        village.ArmyCount += army.Count;
                    if (army.Target is Castle castle)
                        castle.ArmyCount += army.Count;
                }
                // Если деревня не принадлежит владельцу армии
                else
                {
                    // Расчет итогов битвы
                    if (army.Target is Village village)
                    {
                        var battleResult = army.Count - (int)(village.ArmyCount * village.DefenseModifier);
                        if (battleResult > 0)
                        {
                            village.Owner = army.Owner;
                            village.ArmyCount += battleResult;
                        }
                        else
                        {
                            village.ArmyCount = (int)(Math.Abs(battleResult) / village.DefenseModifier);
                        }
                    }
                    if (army.Target is Castle castle)
                    {
                        var battleResult = army.Count - (int)(castle.ArmyCount * castle.DefenseModifier);
                        if (battleResult > 0)
                        {
                            castle.Owner = army.Owner;
                            castle.ArmyCount += battleResult;

                            var winTimer = new Timer(
                                new TimerCallback(GameOver),
                                state: army.Owner,
                                dueTime: 30000,
                                period: Timeout.Infinite);
                            var sessionId = BattleTimers.Single(_ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime).sessionId;
                            if (WinTimers.ContainsKey(sessionId))
                                WinTimers.TryRemove(sessionId, out _);
                            WinTimers.TryAdd(sessionId, winTimer);
                        }
                        else
                        {
                            castle.ArmyCount = (int)(Math.Abs(battleResult) / castle.DefenseModifier);
                        }
                    }

                    // Удаляем использованный таймер
                    BattleTimers.Remove(
                            BattleTimers.Single(_ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime));
                }
            }
            // Если столкнулись с вражеской армией
            else
            {
                // Для каждой вражеской армии проверяем
                foreach (var enemyArmy in army.Road.Armies.Where(_ => _.Owner.Id != army.Owner.Id))
                {
                    // Совпадает ли дистанция, которую ей осталось пройти с дистанцией которую прошла наша армия
                    var armyPreviousDistance = ((currentTime - army.StartTime).TotalSeconds / (army.FinishTime - army.StartTime).TotalSeconds) * army.Road.Length;
                    if (((enemyArmy.FinishTime - currentTime).TotalSeconds / (enemyArmy.FinishTime - enemyArmy.StartTime).TotalSeconds) *
                        enemyArmy.Road.Length - armyPreviousDistance < 1)
                    {
                        // Если совпадает, то считаем итоги битвы
                        var battleResult = army.Count - enemyArmy.Count;
                        if (battleResult > 0)
                        {
                            army.Count = battleResult;
                            army.Road.Armies.Remove(enemyArmy);

                            // Добавляем новый таймер до следующей битвы
                            var elapsedBattleTime = (army.FinishTime - currentTime).TotalSeconds;
                            var elapsedDistance = army.Road.Length - armyPreviousDistance;
                            foreach (var enemyArmy2 in army.Road.Armies.Where(_ => _.Owner.Id != army.Owner.Id))
                            {
                                var enemyArmyLastDistance = ((enemyArmy2.FinishTime - currentTime).TotalSeconds / (enemyArmy2.FinishTime - enemyArmy2.StartTime).TotalSeconds) * enemyArmy2.Road.Length;
                                if (enemyArmyLastDistance < armyPreviousDistance)
                                    continue;

                                elapsedBattleTime = Math.Min(elapsedBattleTime, (enemyArmyLastDistance - armyPreviousDistance) /
                                    (enemyArmy2.Road.SpeedModifier * enemyArmy2.SpeedModifier * 1 + army.Road.SpeedModifier * army.SpeedModifier * 1));
                                elapsedDistance = Math.Min(elapsedDistance, enemyArmyLastDistance - armyPreviousDistance);
                            }
                            var battleTimer = new Timer(
                                new TimerCallback(SimulateBattle),
                                state: army,
                                dueTime: (int)elapsedBattleTime * 1000,
                                period: Timeout.Infinite);
                            var sessionId = Sessions.Values.Where(_ => _.Players.Any(_ => _.Key == army.Owner.Id)).Single().Id;
                            BattleTimers.AddLast((army, battleTimer, sessionId));
                        }
                        if (battleResult < 0)
                        {
                            enemyArmy.Count = -battleResult;
                            enemyArmy.Road.Armies.Remove(army);

                            // Добавляем новый таймер до следующей битвы
                            var elapsedBattleTime = (enemyArmy.FinishTime - currentTime).TotalSeconds;
                            var elapsedDistance = armyPreviousDistance;
                            foreach (var enemyArmy2 in army.Road.Armies.Where(_ => _.Owner.Id != enemyArmy.Owner.Id))
                            {
                                var enemyArmyLastDistance = ((enemyArmy2.FinishTime - currentTime).TotalSeconds / (enemyArmy2.FinishTime - enemyArmy2.StartTime).TotalSeconds) * enemyArmy2.Road.Length;
                                if (enemyArmyLastDistance + armyPreviousDistance > enemyArmy.Road.Length)
                                    continue;

                                elapsedBattleTime = Math.Min(elapsedBattleTime, (armyPreviousDistance - (enemyArmy.Road.Length - enemyArmyLastDistance)) /
                                    (enemyArmy2.Road.SpeedModifier * enemyArmy2.SpeedModifier * 1 + enemyArmy.Road.SpeedModifier * enemyArmy.SpeedModifier * 1));
                                elapsedDistance = Math.Min(elapsedDistance, armyPreviousDistance - (enemyArmy.Road.Length - enemyArmyLastDistance));
                            }
                            var battleTimer = new Timer(
                                new TimerCallback(SimulateBattle),
                                state: army,
                                dueTime: (int)elapsedBattleTime * 1000,
                                period: Timeout.Infinite);
                            var sessionId = Sessions.Values.Where(_ => _.Players.Any(_ => _.Key == army.Owner.Id)).Single().Id;
                            BattleTimers.AddLast((army, battleTimer, sessionId));
                        }
                        if (battleResult == 0)
                        {
                            army.Road.Armies.Remove(enemyArmy);
                            army.Road.Armies.Remove(army);
                        }

                        // Удаляем использованные таймеры
                        BattleTimers.Remove(
                                BattleTimers.Single(_ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime));
                        BattleTimers.Remove(
                                BattleTimers.Single(_ => _.army.Owner.Id == enemyArmy.Owner.Id && _.army.StartTime == enemyArmy.StartTime));
                        break;
                    }
                }
            }
        }

        private void GameOver(object? state)
        {
            // Закончилось время
            if (state is Guid sessionId2)
            {
                _ = Sessions.TryGetValue(sessionId2, out var session);
                if (session!.Winner == null)
                {
                    session!.Winner = session.Players.First().Value.Influence > session.Players.Last().Value.Influence
                        ? session.Players.First().Value : session.Players.Last().Value;

                    _ = WinTimers.TryRemove(sessionId2, out _);
                    _ = GameTimers.TryRemove(sessionId2, out _);

                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId2,
                        dueTime: (int)TimeSpan.FromSeconds(10).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId2, gameTimer);
                }
                else
                {
                    _ = GameTimers.TryRemove(sessionId2, out _);
                    _ = Sessions.TryRemove(sessionId2, out _);
                }
            }
            else
            {
                // Замок захвачен
                if (state is Player player)
                {
                    var sessionId = Sessions.Values.Where(_ => _.Players.Any(_ => _.Key == player.Id)).Single().Id;
                    _ = Sessions.TryGetValue(sessionId, out var session);
                    session!.Winner = player;

                    _ = WinTimers.TryRemove(sessionId, out _);
                    _ = GameTimers.TryRemove(sessionId, out _);

                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId,
                        dueTime: (int)TimeSpan.FromSeconds(10).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId, gameTimer);
                }
            }
        }
    }
}