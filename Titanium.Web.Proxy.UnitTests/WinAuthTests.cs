using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Network.WinAuth;

namespace Titanium.Web.Proxy.UnitTests
{
    public class WinAuthTests
    {
        [Test]
        public async Task Test_Acquire_Client_Token()
        {
            var token = WinAuthHandler.GetInitialAuthToken("mylocalserver.com", "NTLM", new InternalDataStore());
            await Assert.That(token.Length).IsGreaterThan(1);
        }
    }
}