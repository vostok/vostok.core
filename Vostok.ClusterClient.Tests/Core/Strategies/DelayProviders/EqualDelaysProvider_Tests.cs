﻿using System;
using FluentAssertions;
using Vostok.Clusterclient.Helpers;
using Vostok.Clusterclient.Model;
using Vostok.Clusterclient.Strategies.DelayProviders;
using Xunit;

namespace Vostok.Clusterclient.Core.Strategies.DelayProviders
{
    public class EqualDelaysProvider_Tests
    {
        private readonly Request request;

        public EqualDelaysProvider_Tests()
        {
            request = Request.Get("/foo");
        }

        [Fact]
        public void Should_throw_an_error_when_given_negative_division_factor()
        {
            Action action = () => new EqualDelaysProvider(-1);

            action.ShouldThrow<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Fact]
        public void Should_throw_an_error_when_given_zero_division_factor()
        {
            Action action = () => new EqualDelaysProvider(0);

            action.ShouldThrow<ArgumentOutOfRangeException>().Which.ShouldBePrinted();
        }

        [Fact]
        public void Should_return_equal_portions_of_total_budget_divided_by_given_factor()
        {
            var provider = new EqualDelaysProvider(3);

            var budget = Budget.WithRemaining(12.Seconds());

            for (var i = 0; i < 10; i++)
            {
                provider.GetForkingDelay(request, budget, i, 10).Should().Be(4.Seconds());
            }
        }

        [Fact]
        public void Should_adjust_division_factor_if_there_are_not_enough_replicas()
        {
            var provider = new EqualDelaysProvider(3);

            var budget = Budget.WithRemaining(12.Seconds());

            provider.GetForkingDelay(request, budget, 0, 2).Should().Be(6.Seconds());
            provider.GetForkingDelay(request, budget, 1, 2).Should().Be(6.Seconds());
        }
    }
}
