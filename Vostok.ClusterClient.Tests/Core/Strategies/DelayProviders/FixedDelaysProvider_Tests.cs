﻿using System;
using FluentAssertions;
using Vostok.Clusterclient.Helpers;
using Vostok.Clusterclient.Model;
using Vostok.Clusterclient.Strategies.DelayProviders;
using Xunit;

namespace Vostok.Clusterclient.Core.Strategies.DelayProviders
{
    public class FixedDelaysProvider_Tests
    {
        private readonly Request request;

        public FixedDelaysProvider_Tests()
        {
            request = Request.Get("/foo");
        }

        [Fact]
        public void Should_throw_an_error_when_given_null_delays_array()
        {
            Action action = () => new FixedDelaysProvider(TailDelayBehaviour.StopIssuingDelays, null);

            action.ShouldThrow<ArgumentNullException>().Which.ShouldBePrinted();
        }

        [Fact]
        public void Should_throw_an_error_when_given_empty_delays_array()
        {
            Action action = () => new FixedDelaysProvider(TailDelayBehaviour.StopIssuingDelays);

            action.ShouldThrow<ArgumentException>().Which.ShouldBePrinted();
        }

        [Fact]
        public void Should_return_all_fixed_delays_one_by_one_without_considering_remaining_time_budget()
        {
            var provider = new FixedDelaysProvider(TailDelayBehaviour.StopIssuingDelays, 5.Seconds(), 3.Seconds(), 10.Seconds());
            var budget = Budget.WithRemaining(1.Seconds());

            provider.GetForkingDelay(request, budget, 0, 5).Should().Be(5.Seconds());
            provider.GetForkingDelay(request, budget, 1, 5).Should().Be(3.Seconds());
            provider.GetForkingDelay(request, budget, 2, 5).Should().Be(10.Seconds());
        }

        [Fact]
        public void Should_correctly_implement_stop_issuing_delays_tail_behaviour()
        {
            var provider = new FixedDelaysProvider(TailDelayBehaviour.StopIssuingDelays, 5.Seconds(), 3.Seconds(), 10.Seconds());

            provider.GetForkingDelay(request, Budget.Infinite, 3, 5).Should().BeNull();
            provider.GetForkingDelay(request, Budget.Infinite, 4, 5).Should().BeNull();
        }

        [Fact]
        public void Should_correctly_implement_repeat_last_value_tail_behaviour()
        {
            var provider = new FixedDelaysProvider(TailDelayBehaviour.RepeatLastValue, 5.Seconds(), 3.Seconds(), 10.Seconds());

            provider.GetForkingDelay(request, Budget.Infinite, 3, 5).Should().Be(10.Seconds());
            provider.GetForkingDelay(request, Budget.Infinite, 4, 5).Should().Be(10.Seconds());
        }

        [Fact]
        public void Should_correctly_implement_repeat_all_values_tail_behaviour()
        {
            var provider = new FixedDelaysProvider(TailDelayBehaviour.RepeatAllValues, 5.Seconds(), 3.Seconds(), 10.Seconds());

            provider.GetForkingDelay(request, Budget.Infinite, 3, 10).Should().Be(5.Seconds());
            provider.GetForkingDelay(request, Budget.Infinite, 4, 10).Should().Be(3.Seconds());
            provider.GetForkingDelay(request, Budget.Infinite, 5, 10).Should().Be(10.Seconds());

            provider.GetForkingDelay(request, Budget.Infinite, 6, 10).Should().Be(5.Seconds());
            provider.GetForkingDelay(request, Budget.Infinite, 7, 10).Should().Be(3.Seconds());
            provider.GetForkingDelay(request, Budget.Infinite, 8, 10).Should().Be(10.Seconds());
        }
    }
}
