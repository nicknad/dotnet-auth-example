using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Tests.Security;

#pragma warning disable CA1515 // Consider making public types internal
public abstract class KestrelTestBase : IClassFixture<KestrelFixture>
#pragma warning restore CA1515 // Consider making public types internal
{
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly HttpClient HttpClient;
    protected readonly KestrelFixture Fixture;
#pragma warning restore CA1051 // Do not declare visible instance fields

    protected KestrelTestBase(KestrelFixture fixture) {
        Fixture = fixture;
#pragma warning disable CA1062 // Validate arguments of public methods
        HttpClient = fixture.Client;
#pragma warning restore CA1062 // Validate arguments of public methods
    }
}
