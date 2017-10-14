﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Vostok.Clusterclient.Criteria;
using Vostok.Clusterclient.Helpers;
using Vostok.Clusterclient.Misc;
using Vostok.Clusterclient.Model;
using Vostok.Clusterclient.Modules;
using Vostok.Clusterclient.Transport;
using Xunit;

namespace Vostok.Clusterclient.Core.Modules
{
    public class AbsoluteUrlSenderModule_Tests
    {
        private readonly ITransport transport;
        private readonly IResponseClassifier responseClassifier;
        private readonly IList<IResponseCriterion> responseCriteria;
        private readonly IClusterResultStatusSelector resultStatusSelector;

        private readonly IRequestContext context;

        private readonly AbsoluteUrlSenderModule module;
        private Request request;
        private Response response;

        public AbsoluteUrlSenderModule_Tests()
        {
            request = Request.Get("http://foo/bar");
            response = new Response(ResponseCode.Ok);

            var budget = Budget.WithRemaining(5.Seconds());

            context = Substitute.For<IRequestContext>();
            context.Request.Returns(_ => request);
            context.Budget.Returns(_ => budget);

            transport = Substitute.For<ITransport>();
            transport.SendAsync(Arg.Any<Request>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).ReturnsTask(_ => response);

            responseCriteria = new List<IResponseCriterion>();
            responseClassifier = Substitute.For<IResponseClassifier>();
            responseClassifier.Decide(Arg.Any<Response>(), Arg.Any<IList<IResponseCriterion>>()).Returns(ResponseVerdict.Accept);

            resultStatusSelector = Substitute.For<IClusterResultStatusSelector>();
            resultStatusSelector.Select(null, null).ReturnsForAnyArgs(ClusterResultStatus.Success);

            module = new AbsoluteUrlSenderModule(transport, responseClassifier, responseCriteria, resultStatusSelector);
        }

        [Fact]
        public void Should_delegate_to_next_module_when_request_url_is_relative()
        {
            request = Request.Get("foo/bar");

            var result = new ClusterResult(ClusterResultStatus.Success, new List<ReplicaResult>(), response, request);

            Execute(result).Should().BeSameAs(result);

            transport.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public void Should_send_request_using_transport_directly_if_url_is_absolute()
        {
            Execute();

            transport.Received().SendAsync(request, 5.Seconds(), context.CancellationToken);
        }

        [Fact]
        public void Should_return_canceled_result_if_transport_returns_a_canceled_response()
        {
            response = new Response(ResponseCode.Canceled);

            Execute().Status.Should().Be(ClusterResultStatus.Canceled);
        }

        [Fact]
        public void Should_classify_response_to_obtain_a_verdict()
        {
            Execute();

            responseClassifier.Received().Decide(response, responseCriteria);
        }

        [Theory]
        [InlineData(ClusterResultStatus.Success)]
        [InlineData(ClusterResultStatus.TimeExpired)]
        [InlineData(ClusterResultStatus.ReplicasExhausted)]
        public void Should_return_result_with_status_given_by_result_status_selector(ClusterResultStatus status)
        {
            resultStatusSelector.Select(null, null).ReturnsForAnyArgs(status);

            Execute().Status.Should().Be(status);
        }

        [Fact]
        public void Should_return_result_with_received_response_from_transport()
        {
            Execute().Response.Should().BeSameAs(response);
        }

        [Fact]
        public void Should_return_result_with_a_single_correct_replica_result()
        {
            var replicaResult = Execute().ReplicaResults.Should().ContainSingle().Which;

            replicaResult.Replica.Should().BeSameAs(request.Url);
            replicaResult.Response.Should().BeSameAs(response);
            replicaResult.Verdict.Should().Be(ResponseVerdict.Accept);
        }

        private ClusterResult Execute(ClusterResult result = null)
        {
            return module.ExecuteAsync(context, _ => Task.FromResult(result)).GetAwaiter().GetResult();
        }
    }
}
