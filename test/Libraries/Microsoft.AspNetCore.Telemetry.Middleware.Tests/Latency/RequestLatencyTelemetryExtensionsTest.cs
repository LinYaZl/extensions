﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class RequestLatencyTelemetryExtensionsTest
{
    [Fact]
    public void RequestLatencyExtensions_NullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
        RequestLatencyTelemetryExtensions.AddRequestLatencyTelemetry(null!));
        Assert.Throws<ArgumentNullException>(() =>
        RequestLatencyTelemetryExtensions.AddRequestLatencyTelemetry(new ServiceCollection(), configure: null!));
        Assert.Throws<ArgumentNullException>(() =>
        RequestLatencyTelemetryExtensions.AddRequestLatencyTelemetry(new ServiceCollection(), section: null!));
        Assert.Throws<ArgumentNullException>(() =>
        RequestLatencyTelemetryExtensions.UseRequestLatencyTelemetry(null!));
    }

    [Fact]
    public void RequestLatencyExtensions_AddRequestLatency_AddsMiddleware()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext()
            .AddRequestLatencyTelemetry()
            .BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<RequestLatencyTelemetryMiddleware>());
    }

    [Fact]
    public void RequestLatencyExtensions_AddRequestLatency_AddsLatencyContext()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext()
            .AddRequestLatencyTelemetry()
            .BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ILatencyContext>());

        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        Assert.Equal(scope1.ServiceProvider.GetService<ILatencyContext>(),
            scope1.ServiceProvider.GetService<ILatencyContext>());
        Assert.NotEqual(scope1.ServiceProvider.GetService<ILatencyContext>(),
            scope2.ServiceProvider.GetService<ILatencyContext>());
    }

    [Fact]
    public void RequestLatencyExtensions_AddRequestLatency_InvokesConfig()
    {
        bool invoked = false;
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext()
            .AddRequestLatencyTelemetry(a => { invoked = true; })
            .BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<RequestLatencyTelemetryMiddleware>());
        Assert.True(invoked);
    }

    [Fact]
    public void RequestLatencyExtensions_Add_BindsToConfigSection()
    {
        RequestLatencyTelemetryOptions expectedOptions = new()
        {
            LatencyDataExportTimeout = TimeSpan.FromSeconds(2)
        };
        var config = GetConfigSection(expectedOptions);

        using var serviceProvider = new ServiceCollection()
            .AddRequestLatencyTelemetry(config)
            .BuildServiceProvider();

        var actualOptions = serviceProvider.GetRequiredService<IOptions<RequestLatencyTelemetryOptions>>();

        Assert.True(actualOptions.Value.LatencyDataExportTimeout == expectedOptions.LatencyDataExportTimeout);
    }

    private static IConfigurationSection GetConfigSection(RequestLatencyTelemetryOptions options)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(RequestLatencyTelemetryOptions)}:{nameof(options.LatencyDataExportTimeout)}", options.LatencyDataExportTimeout.ToString() },
            })
            .Build()
            .GetSection($"{nameof(RequestLatencyTelemetryOptions)}");
    }
}
