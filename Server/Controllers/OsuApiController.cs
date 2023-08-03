using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UREstimator.Server.OsuApi;
using UREstimator.Shared;

namespace UREstimator.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OsuApiController : ControllerBase
    {
        private readonly OsuApiProvider _provider;

        public OsuApiController(OsuApiProvider provider)
        {
            _provider = provider;
        }

        [HttpGet("player/{username}/{mode}")]
        public Task<Score[]?> GetPlayer([Required] string username, [Required] string mode)
        {
            return _provider.GetPlayerScores(username, mode);
        }

        [HttpGet("score/{id}/{mode}")]
        public Task<Score?> GetScore([Required] long id, [Required] string mode)
        {
            return _provider.GetScore(id, mode);
        }

        [HttpGet("leaderboard/{mode}")]
        public Task<LeaderboardPlayer[]?> GetLeaderboard([Required] string mode)
        {
            return _provider.GetLeaderboard(mode);
        }
    }
}