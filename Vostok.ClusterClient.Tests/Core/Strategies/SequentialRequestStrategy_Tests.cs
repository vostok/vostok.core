﻿using System;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using Vostok.Clusterclient.Helpers;
using Vostok.Clusterclient.Model;
using Vostok.Clusterclient.Sending;
using Vostok.Clusterclient.Strategies;
using Vostok.Clusterclient.Strategies.TimeoutProviders;
using Xunit;

namespace Vostok.Clusterclient.Core.Strategies
{
    public class SequentialRequestStrategy_Tests
    {
        private readonly Uri replica1;
        private readonly Uri replica2;
        private readonly Uri replica3;
        private readonly Uri[] replicas;
        private readonly Request request;

        private readonly ISequentialTimeoutsProvider timeoutsProvider;
        private readonly IRequestSender sender;
        private readonly CancellationToken token;

        private readonly SequentialRequestStrategy strategy;

        public SequentialRequestStrategy_Tests()
        {
            replica1 = new Uri("http://replica1/");
            replica2 = new Uri("http://replica2/");
            replica3 = new Uri("http://replica3/");
            replicas = new[] {replica1, replica2, replica3};

            request = Request.Get("/foo");

            timeoutsProvider = Substitute.For<ISequentialTimeoutsProvider>();
            timeoutsProvider.GetTimeout(null, null, 0, 0).ReturnsForAnyArgs(1.Seconds(), 2.Seconds(), 3.Seconds());

            sender = Substitute.For<IRequestSender>();
            SetupResult(replica1, ResponseVerdict.Reject);
            SetupResult(replica2, ResponseVerdict.Reject);
            SetupResult(replica3, ResponseVerdict.Reject);

            token = new CancellationTokenSource().Token;

            strategy = new SequentialRequestStrategy(timeoutsProvider);
        }

        [Fact]
        public void Should_not_make_any_requests_when_time_budget_is_expired()
        {
            Send(Budget.Expired);

            sender.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public void Should_consult_timeout_provider_for_each_replica_with_correct_parameters()
        {
            Send(Budget.Infinite);

            timeoutsProvider.ReceivedCalls().Should().HaveCount(3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 0, 3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 1, 3);
            timeoutsProvider.Received(1).GetTimeout(request, Budget.Infinite, 2, 3);
        }

        [Fact]
        public void Should_send_request_to_each_replica_with_correct_timeout()
        {
            Send(Budget.Infinite);

            sender.ReceivedCalls().Should().HaveCount(3);
            sender.Received(1).SendToReplicaAsync(replica1, request, 1.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, 2.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica3, request, 3.Seconds(), token);
        }

        [Fact]
        public void Should_limit_request_timeouts_by_remaining_time_budget()
        {
            Send(Budget.WithRemaining(1500.Milliseconds()));

            sender.ReceivedCalls().Should().HaveCount(3);
            sender.Received(1).SendToReplicaAsync(replica1, request, 1.Seconds(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, 1500.Milliseconds(), token);
            sender.Received(1).SendToReplicaAsync(replica3, request, 1500.Milliseconds(), token);
        }

        [Fact]
        public void Should_stop_at_first_accepted_result()
        {
            SetupResult(replica2, ResponseVerdict.Accept);

            Send(Budget.Infinite);

            sender.ReceivedCalls().Should().HaveCount(2);
            sender.Received(1).SendToReplicaAsync(replica1, request, Arg.Any<TimeSpan>(), token);
            sender.Received(1).SendToReplicaAsync(replica2, request, Arg.Any<TimeSpan>(), token);
            sender.DidNotReceive().SendToReplicaAsync(replica3, request, Arg.Any<TimeSpan>(), token);
        }

        private void SetupResult(Uri replica, ResponseVerdict verdict)
        {
            sender
                .SendToReplicaAsync(replica, Arg.Any<Request>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .ReturnsTask(new ReplicaResult(replica, new Response(ResponseCode.Ok), verdict, TimeSpan.Zero));
        }

        private void Send(IRequestTimeBudget budget)
        {
            strategy.SendAsync(request, sender, budget, replicas, replicas.Length, token).GetAwaiter().GetResult();
        }
    }
}
