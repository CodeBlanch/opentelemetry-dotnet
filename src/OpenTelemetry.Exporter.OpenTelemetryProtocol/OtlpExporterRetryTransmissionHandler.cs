// <copyright file="OtlpExporterRetryTransmissionHandler.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using Grpc.Core;

namespace OpenTelemetry.Exporter;

// Implements the OpenTelemetry Specification in-memory retry logic for OtlpExporter
public class OtlpExporterRetryTransmissionHandler<T> : OtlpExporterTransmissionHandler<T>
{
    protected override bool OnSubmitRequestExceptionThrown(T request, Exception exception)
    {
        var retryAttemptCount = 0;

        while (this.ShouldRetryRequest(request, exception, retryAttemptCount++, out var sleepDuration))
        {
            if (sleepDuration > TimeSpan.Zero)
            {
                Thread.Sleep(sleepDuration);
            }

            if (this.RetryRequest(request, out exception))
            {
                return true;
            }
        }

        return this.OnHandleDroppedRequest(request);
    }

    protected virtual bool ShouldRetryRequest(T request, Exception exception, int retryAttemptCount, out TimeSpan sleepDuration)
    {
        if (exception is RpcException rpcException
            && OtlpRetry.TryGetGrpcRetryResult(rpcException.StatusCode, this.Options.Deadline, rpcException.Trailers, retryAttemptCount, out var retryResult))
        {
            sleepDuration = retryResult.RetryDelay;
            return true;
        }

        sleepDuration = default;
        return false;
    }
}
