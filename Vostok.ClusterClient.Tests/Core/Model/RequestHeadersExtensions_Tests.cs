﻿using System;
using System.Globalization;
using System.Text;
using System.Threading;
using FluentAssertions;
using Vostok.Clusterclient.Model;
using Xunit;

// ReSharper disable PossibleNullReferenceException

namespace Vostok.Clusterclient.Core.Model
{
    public class RequestHeadersExtensions_Tests
    {
        private Request request;

        public RequestHeadersExtensions_Tests()
        {
            request = Request.Get("foo/bar");
        }

        [Fact]
        public void WithAcceptHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithAcceptHeader("application/json");

            request.Headers.Accept.Should().Be("application/json");
        }

        [Fact]
        public void WithAcceptCharsetHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithAcceptCharsetHeader("utf-8");

            request.Headers[HeaderNames.AcceptCharset].Should().Be("utf-8");
        }

        [Fact]
        public void WithAcceptCharsetHeader_should_set_correct_value_when_given_an_encoding_argument()
        {
            request = request.WithAcceptCharsetHeader(Encoding.UTF8);

            request.Headers[HeaderNames.AcceptCharset].Should().Be("utf-8");
        }

        [Fact]
        public void WithAcceptEncodingHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithAcceptEncodingHeader("gzip");

            request.Headers[HeaderNames.AcceptEncoding].Should().Be("gzip");
        }

        [Fact]
        public void WithAuthorizationHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithAuthorizationHeader("Basic QWxhZGRpbjpPcGVuU2VzYW1l");

            request.Headers.Authorization.Should().Be("Basic QWxhZGRpbjpPcGVuU2VzYW1l");
        }

        [Fact]
        public void WithAuthorizationHeader_should_set_correct_value_when_given_scheme_and_parameter_arguments()
        {
            request = request.WithAuthorizationHeader("Basic", "QWxhZGRpbjpPcGVuU2VzYW1l");

            request.Headers.Authorization.Should().Be("Basic QWxhZGRpbjpPcGVuU2VzYW1l");
        }

        [Fact]
        public void WithBasicAuthorizationHeader_should_set_correct_value_when_given_user_and_password_arguments()
        {
            request = request.WithBasicAuthorizationHeader("Aladdin", "OpenSesame");

            request.Headers.Authorization.Should().Be("Basic QWxhZGRpbjpPcGVuU2VzYW1l");
        }

        [Fact]
        public void WithContentEncodingHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithContentEncodingHeader("gzip");

            request.Headers.ContentEncoding.Should().Be("gzip");
        }

        [Fact]
        public void WithContentTypeHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithContentTypeHeader("text/plain");

            request.Headers.ContentType.Should().Be("text/plain");
        }

        [Fact]
        public void WithContentRangeHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithContentRangeHeader("bytes 0-499/1234");

            request.Headers.ContentRange.Should().Be("bytes 0-499/1234");
        }

        [Fact]
        public void WithContentRangeHeader_should_set_correct_value_when_length_argument()
        {
            request = request.WithContentRangeHeader(12);

            request.Headers.ContentRange.Should().Be("bytes */12");
        }

        [Fact]
        public void WithContentRangeHeader_should_set_correct_value_when_from_and_to_arguments()
        {
            request = request.WithContentRangeHeader(12, 15);

            request.Headers.ContentRange.Should().Be("bytes 12-15/*");
        }

        [Fact]
        public void WithContentRangeHeader_should_set_correct_value_when_from_to_and_length_arguments()
        {
            request = request.WithContentRangeHeader(12, 15, 100);

            request.Headers.ContentRange.Should().Be("bytes 12-15/100");
        }

        [Fact]
        public void WithIfMatchHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithIfMatchHeader("\"xyzzy\"");

            request.Headers[HeaderNames.IfMatch].Should().Be("\"xyzzy\"");
        }

        [Fact]
        public void WithIfNoneMatchHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithIfNoneMatchHeader("\"xyzzy\"");

            request.Headers[HeaderNames.IfNoneMatch].Should().Be("\"xyzzy\"");
        }

        [Fact]
        public void WithIfModifiedSinceHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithIfModifiedSinceHeader("Sat, 29 Oct 1994 19:43:31 GMT");

            request.Headers[HeaderNames.IfModifiedSince].Should().Be("Sat, 29 Oct 1994 19:43:31 GMT");
        }

        [Fact]
        public void WithIfModifiedSinceHeader_should_set_correct_value_when_given_a_datetime_argument()
        {
            var value = new DateTime(1994, 10, 29, 19, 43, 31, DateTimeKind.Utc);

            request = request.WithIfModifiedSinceHeader(value);

            request.Headers[HeaderNames.IfModifiedSince].Should().Be("Sat, 29 Oct 1994 19:43:31 GMT");
        }

        [Fact]
        public void WithRangeHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithRangeHeader("bytes=500-999");

            request.Headers.Range.Should().Be("bytes=500-999");
        }

        [Theory]
        [InlineData(10L, 15L, "bytes=10-15")]
        [InlineData(10L, null, "bytes=10-")]
        [InlineData(null, 15L, "bytes=-15")]
        public void WithRangeHeader_should_set_correct_value_when_given_from_and_to_arguments(long? from, long? to, string expected)
        {
            request = request.WithRangeHeader(from, to);

            request.Headers.Range.Should().Be(expected);
        }

        [Fact]
        public void WithUserAgentHeader_should_set_correct_value_when_given_a_string_argument()
        {
            request = request.WithUserAgentHeader("CERN-LineMode/2.15 libwww/2.17b3");

            request.Headers.UserAgent.Should().Be("CERN-LineMode/2.15 libwww/2.17b3");
        }

        [Fact]
        public void AppendToHeaderWithQuality_should_produce_correct_header_value()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

            request = request
                .AppendToHeaderWithQuality(HeaderNames.Accept, "foo", 0.4m)
                .AppendToHeaderWithQuality(HeaderNames.Accept, "bar", 0.5m);

            request.Headers[HeaderNames.Accept].Should().Be("bar;q=0.5,foo;q=0.4");
        }
    }
}
