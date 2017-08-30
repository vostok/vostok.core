﻿using FluentAssertions;
using Vostok.Clusterclient.Criteria;
using Vostok.Clusterclient.Model;
using Xunit;

namespace Vostok.Clusterclient.Core.Criteria
{
    public class AcceptNonRetriableCriterion_Tests
    {
        private readonly AcceptNonRetriableCriterion criterion;

        public AcceptNonRetriableCriterion_Tests()
        {
            criterion = new AcceptNonRetriableCriterion();
        }

        [Fact]
        public void Should_accept_an_error_response_with_dont_retry_header()
        {
            var response = new Response(ResponseCode.ServiceUnavailable, headers: Headers.Empty.Set(HeaderNames.XKonturDontRetry, ""));

            criterion.Decide(response).Should().Be(ResponseVerdict.Accept);
        }

        [Fact]
        public void Should_know_nothing_about_an_error_response_without_dont_retry_header()
        {
            var response = new Response(ResponseCode.ServiceUnavailable, headers: Headers.Empty);

            criterion.Decide(response).Should().Be(ResponseVerdict.DontKnow);
        }
    }
}
