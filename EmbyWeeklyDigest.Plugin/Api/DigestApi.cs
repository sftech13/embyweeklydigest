using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace EmbyWeeklyDigest.Plugin.Api
{
    [Authenticated(Roles = "Admin")]
    [Route("/EmbyWeeklyDigest/SendNow", "POST", Summary = "Build and send the digest immediately (for testing)")]
    public class SendDigestNow : IReturn<DigestSendResult>
    {
        public int LookbackDays { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyWeeklyDigest/Digests", "GET", Summary = "List sent digests with per-user delivery status")]
    public class GetDigests : IReturn<List<PendingDigest>> { }

    [Authenticated(Roles = "Admin")]
    [Route("/EmbyWeeklyDigest/Digests/{Id}", "DELETE", Summary = "Dismiss a digest")]
    public class DismissDigest : IReturn<object>
    {
        public string Id { get; set; }
    }

    public class DigestApi : IService, IRequiresRequest
    {
        public IRequest Request { get; set; }

        public async Task<object> Post(SendDigestNow request)
        {
            var days = request.LookbackDays > 0 ? request.LookbackDays : 7;
            return await Plugin.Instance.SendDigestAsync(days).ConfigureAwait(false);
        }

        public object Get(GetDigests request)
        {
            return Plugin.Instance.Store.GetAll();
        }

        public object Delete(DismissDigest request)
        {
            Plugin.Instance.Store.Dismiss(request.Id);
            return new { Success = true };
        }
    }
}
