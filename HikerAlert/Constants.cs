using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlert
{
    //Separated from MauiProgram.cs to avoid circular dependency on HikerAlertApp.Core
    static class Constants
    {
        public const string RangerSosEndpoint = "https://your-ranger-api.example/sos";
    }
}
