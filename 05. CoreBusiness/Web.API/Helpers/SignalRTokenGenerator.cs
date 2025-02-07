using System;
using System.Security.Claims;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Web.Core.Business.API.Helpers
{
    public static class SignalRTokenGenerator
    {
        public static string GenerateToken(string connectionString, string hubName)
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(o => o.ConnectionString = connectionString).Build();
            return serviceManager.GenerateClientAccessToken(hubName);
        }
    }

}
