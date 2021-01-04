using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace Toad.Controllers
{
    [ApiController]
    [Route("releases")]
    public sealed class ReleasesController : Controller
    {
        public sealed class CachedRelease
        {
            public class Asset
            {
                public string Name { get; set; }
                public string Url { get; set; }
            }

            public IEnumerable<Asset> Assets { get; set; }
            public string Url { get; set; }
            public string Version { get; set; }
            public string Notes { get; set; }
        }

        private readonly IMemoryCache _memoryCache;

        public ReleasesController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestAsync()
        {
            CachedRelease release;

            if (!_memoryCache.TryGetValue("CachedRelease", out release))
            {
                var client = new GitHubClient(new ProductHeaderValue("PicoTorrentApi", "1.0"));
                var remoteRelease = await client.Repository.Release.GetLatest("picotorrent", "picotorrent");

                release = new CachedRelease
                {
                    Assets = remoteRelease.Assets.Select(a =>
                        new CachedRelease.Asset { Name = a.Name, Url = a.BrowserDownloadUrl }),
                    Notes = await client.Miscellaneous.RenderArbitraryMarkdown(
                        new NewArbitraryMarkdown(remoteRelease.Body, "gfm", "picotorrent/picotorrent")),
                    Url = remoteRelease.HtmlUrl,
                    Version = remoteRelease.TagName.Substring(1)
                };

                _memoryCache.Set("CachedRelease", release, TimeSpan.FromMinutes(30));
            }

            return Ok(release);
        }
    }
}
